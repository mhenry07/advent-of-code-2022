use std::error::Error;
use std::fs::File;
use std::io::{self, BufRead};
use std::path::Path;

/// Solves Day 3 from a file path `filename`
pub fn run<P>(filename: P) -> Result<Results, Box<dyn Error>>
where P: AsRef<Path> {
    let mut results = Results::new();
    let mut group = Group::new();
    let mut i = 0;
    let lines = read_lines(filename)?;
    for line in lines {
        let num = i % 3;
        group.add_sack(num, line?);
        if num == 2 {
            results.handle_group(&group)?;
        }
        i += 1;
    }

    Ok(results)
}

/// Solves Day 3 from a string slice `input`
pub fn run_lines(input: &str) -> Result<Results, Box<dyn Error>> {
    let mut results = Results::new();
    let mut group = Group::new();
    let mut i = 0;
    for line in input.lines() {
        let num = i % 3;
        group.add_sack(num, line.to_string()); // TODO: find a way to eliminate to_string() if possible
        if num == 2 {
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
            .ok_or_else(|| "No badge found for group")?;

        self.sum_2 += group.badge_priority()
            .ok_or_else(|| "No badge found for group")?;

        Ok(())
    }
}

struct Group {
    sack_1: Option<String>,
    sack_2: Option<String>,
    sack_3: Option<String>
}

impl Group {
    fn new() -> Group {
        Group { sack_1: None, sack_2: None, sack_3: None }
    }

    fn add_sack(&mut self, num: i32, sack: String) {
        if num == 0 {
            self.sack_1 = Some(sack);
        } else if num == 1 {
            self.sack_2 = Some(sack);
        } else if num == 2 {
            self.sack_3 = Some(sack);
        }
    }

    fn get_sack(&self, num: i32) -> Option<&str> {
        let sack = match num {
            0 => &self.sack_1,
            1 => &self.sack_2,
            2 => &self.sack_3,
            _ => &None
        };

        return match sack {
            Some(s) => Some(s.as_str()),
            None => None
        }
    }

    fn compartment_priorities_sum(&self) -> Option<i32> {
        let mut sum = 0;
        for i in 0..3 {
            sum += self.both_compartments_priority(i)?;
        }

        Some(sum)
    }

    fn both_compartments_priority(&self, num: i32) -> Option<i32> {
        let sack = self.get_sack(num)?;
        let (left, right) = sack.split_at(sack.len() / 2);
        let common_item = find_common_compartment_item(left, right)?;

        Some(priority(common_item)?)
    }

    fn badge_priority(&self) -> Option<i32> {
        let badge = self.find_badge()?;
        let priority = priority(badge)?;

        Some(priority)
    }

    fn find_badge(&self) -> Option<char> {
        for i1 in self.get_sack(0)?.chars() {
            for i2 in self.get_sack(1)?.chars() {
                if i2 != i1 {
                    continue;
                }
                for i3 in self.get_sack(2)?.chars() {
                    if i3 == i2 && i2 == i1 {
                        return Some(i1)
                    }
                }
            }
        }

        None
    }
}

fn find_common_compartment_item(first: &str, second: &str) -> Option<char> {
    for f in first.chars() {
        for s in second.chars() {
            if f == s {
                return Some(f)
            }
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
