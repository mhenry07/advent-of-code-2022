use std::error::Error;
use std::fs::File;
use std::io::{self, BufRead};
use std::path::Path;

/// Solves Day 3 from a file path `filename`
pub fn run<P>(filename: P) -> Result<Results, Box<dyn Error>>
where P: AsRef<Path> {
    let mut results = Results::new();
    let mut i = 0;
    let mut group_lines = [String::from(""), String::from(""), String::from("")];
    let lines = read_lines(filename)?;
    for line in lines {
        let num = i % 3;
        group_lines[num] = line?;
        if num == 2 {
            let group = Group::new(group_lines[0].as_str(), group_lines[1].as_str(), group_lines[2].as_str());
            results.handle_group(&group)?;
        }
        i += 1;
    }

    Ok(results)
}

/// Solves Day 3 from a string slice `input`
pub fn run_lines(input: &str) -> Result<Results, Box<dyn Error>> {
    let mut results = Results::new();
    let mut i = 0;
    let mut group_lines = [""; 3];
    for line in input.lines() {
        let num = i % 3;
        group_lines[num] = line;
        if num == 2 {
            let group = Group::new(group_lines[0], group_lines[1], group_lines[2]);
            results.handle_group(&group)?;
        }
        i += 1;
    }

    Ok(results)
}

#[derive(Debug)]
pub struct Results
{
    sum_1: i32,
    sum_2: i32
}

impl Results {
    fn new() -> Results {
        Results { sum_1: 0, sum_2: 0 }
    }

    fn handle_group(&mut self, group: &Group) -> Result<(), Box<dyn Error>> {
        self.sum_1 += group.compartment_priorities_sum()
            .ok_or_else(|| "Common compartment item not found for one or more rucksacks in group")?;

        self.sum_2 += group.badge_priority()
            .ok_or_else(|| "Badge not found for group")?;

        Ok(())
    }
}

// it's more complex to use lifetimes `&'a str` vs. String, but it allows us to avoid string copies from run_lines
struct Group<'a> {
    sack_1: &'a str,
    sack_2: &'a str,
    sack_3: &'a str
}

impl<'a> Group<'a> {
    fn new(sack_1: &'a str, sack_2: &'a str, sack_3: &'a str) -> Group<'a> {
        Group::<'a> { sack_1, sack_2, sack_3 }
    }

    fn get_sack(&self, num: usize) -> &str {
        match num {
            0 => self.sack_1,
            1 => self.sack_2,
            2 => self.sack_3,
            _ => panic!("Invalid num: {num}")
        }
    }

    fn compartment_priorities_sum(&self) -> Option<i32> {
        let mut sum = 0;
        for i in 0..3 {
            sum += self.both_compartments_priority(i)?;
        }

        Some(sum)
    }

    fn both_compartments_priority(&self, num: usize) -> Option<i32> {
        let sack = self.get_sack(num);
        let (first, last) = sack.split_at(sack.len() / 2);
        let common_item = find_common_compartment_item(first, last)?;

        Some(priority(common_item)?)
    }

    fn badge_priority(&self) -> Option<i32> {
        let badge = self.find_badge()?;
        let priority = priority(badge)?;

        Some(priority)
    }

    fn find_badge(&self) -> Option<char> {
        for item in self.sack_1.chars() {
            if self.sack_2.contains(item) && self.sack_3.contains(item) {
                return Some(item)
            }
        }

        None
    }
}

fn find_common_compartment_item(first: &str, last: &str) -> Option<char> {
    for item in first.chars() {
        if last.contains(item) {
            //println!("common item: {item}, first: {first}, last: {last}");
            return Some(item)
        }
    }

    None
}

fn priority(c: char) -> Option<i32> {
    match c {
        'a'..='z' => Some((c as u32 - 'a' as u32 + 1) as i32),
        'A'..='Z' => Some((c as u32 - 'A' as u32 + 27) as i32),
        _ => None
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
vJrwpWtwJgWrhcsFMMfFFhFp
jqHRNqRjqzjGDLGLrsFMfFZSrLrFZsSL
PmmdzqPrVvPwwTWBwg
wMqvLMZHhHMvwLHjbvcjnnSBnvTQFn
ttgJtRGJQctTZtZT
CrZsJsPPZsGzwwsLwLmpwMDw";

    #[test]
    fn part_1() {
        let results = run_lines(EXAMPLE).unwrap();

        assert_eq!(results.sum_1, 157)
    }

    #[test]
    fn part_2() {
        let results = run_lines(EXAMPLE).unwrap();

        assert_eq!(results.sum_2, 70)
    }
}
