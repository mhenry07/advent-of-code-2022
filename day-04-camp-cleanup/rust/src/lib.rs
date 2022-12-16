use std::error::Error;
use std::fs::File;
use std::io::{self, BufRead};
use std::path::Path;
use std::str::FromStr;

/// Solves Day 4 from a file path `filename`
pub fn run<P>(filename: P) -> Result<Results, Box<dyn Error>>
where P: AsRef<Path> {
    let mut results = Results::new();
    let lines = read_lines(filename)?;
    for l in lines {
        let line = l?;
        let pair = line.parse::<AssignmentPair>().unwrap();
        results.handle_pair(&pair);
    }

    Ok(results)
}

/// Solves Day 4 from a string slice `input`
pub fn run_lines(input: &str) -> Result<Results, Box<dyn Error>> {
    let mut results = Results::new();
    for line in input.lines() {
        let pair = line.parse::<AssignmentPair>().unwrap();
        results.handle_pair(&pair);
    }

    Ok(results)
}

#[derive(Debug)]
pub struct Results
{
    count_1: i32,
    count_2: i32
}

impl Results {
    fn new() -> Results {
        Results { count_1: 0, count_2: 0 }
    }

    fn handle_pair(&mut self, pair: &AssignmentPair) {
        let fully_contains = pair.elf_1.fully_contains(&pair.elf_2) || pair.elf_2.fully_contains(&pair.elf_1);
        if fully_contains {
            self.count_1 += 1;
        }

        let overlaps = pair.elf_1.overlaps(&pair.elf_2) || pair.elf_2.overlaps(&pair.elf_1);
        if overlaps {
            self.count_2 += 1;
        }
    }
}

struct AssignmentPair {
    elf_1: Assignment,
    elf_2: Assignment
}

#[derive(Debug)]
struct AssignmentPairError;

impl FromStr for AssignmentPair {
    type Err = AssignmentPairError;

    fn from_str(s: &str) -> Result<Self, Self::Err> {
        let (first, second) = s.split_once(',').ok_or(AssignmentPairError)?;

        Ok(AssignmentPair {
            elf_1: first.parse::<Assignment>()?,
            elf_2: second.parse::<Assignment>()?
        })
    }
}

struct Assignment {
    start: i32,
    end: i32
}

impl Assignment {
    fn fully_contains(&self, other: &Assignment) -> bool {
        self.start <= other.start && other.end <= self.end
    }

    fn overlaps(&self, other: &Assignment) -> bool {
        self.start <= other.start && other.start <= self.end ||
        self.start <= other.end && other.end <= self.end
    }
}

impl FromStr for Assignment {
    type Err = AssignmentPairError;

    fn from_str(s: &str) -> Result<Self, Self::Err> {
        let (start_text, end_text) = s.split_once('-').ok_or(AssignmentPairError)?;
        let start = start_text.parse::<i32>().map_err(|_| AssignmentPairError)?;
        let end = end_text.parse::<i32>().map_err(|_| AssignmentPairError)?;
    
        Ok(Assignment { start, end })
    }
}

// from https://doc.rust-lang.org/rust-by-example/std_misc/file/read_lines.html
fn read_lines<P>(filename: P) -> io::Result<io::Lines<io::BufReader<File>>>
where P: AsRef<Path>, {
    let file = File::open(filename)?;
    Ok(io::BufReader::new(file).lines())
}

#[cfg(test)]
mod tests {
    use crate::run_lines;

    const EXAMPLE: &'static str = "\
2-4,6-8
2-3,4-5
5-7,7-9
2-8,3-7
6-6,4-6
2-6,4-8
";

    #[test]
    fn part_1() {
        let results = run_lines(EXAMPLE).unwrap();

        assert_eq!(results.count_1, 2)
    }

    #[test]
    fn part_2() {
        let results = run_lines(EXAMPLE).unwrap();

        assert_eq!(results.count_2, 4)
    }
}
