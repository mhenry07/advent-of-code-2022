use std::error::Error;
use std::fs::File;
use std::io::BufRead;
use std::path::Path;
use std::{fmt, process, io};

struct TopCalories {
    top: Vec<i32>
}

impl TopCalories {
    fn new(capacity: usize) -> Self {
        TopCalories {
            top: vec![0; capacity]
        }
    }

    fn add_if_top(&mut self, calories: i32) -> Option<i32> {
        let mut ranking: Option<i32> = None;
        let mut prev_top_i = 0;
        let top = &mut self.top;
        for i in 0..top.len() {
            let top_i = top[i];
            match ranking {
                Some(_) => top[i] = prev_top_i,
                None => {
                    if calories > top_i {
                        top[i] = calories;
                        ranking = Some(1 + i as i32);
                    }
                }
            }
            prev_top_i = top_i;
        }

        ranking
    }
}

impl fmt::Display for TopCalories {
    fn fmt(&self, f: &mut fmt::Formatter) -> fmt::Result {
        let n = self.top.len();
        let sum: i32 = self.top.iter().sum();
        write!(f, "Top Calories: Top 1: {}, Total of Top {}: {}", self.top[0], n, sum)
    }
}

fn main() {
    let filename = "../input.txt";

    let result = run(filename);
    match result {
        Ok(top) => {
            println!("{}", top);
        },
        Err(e) => {
            println!("Application error: {e}");
            process::exit(1);
        }
    }
}

fn run<P>(filename: P) -> Result<TopCalories, Box<dyn Error>>
where P: AsRef<Path> {
    let mut top_calories = TopCalories::new(3);
    let mut elf_calories = 0;
    let lines = read_lines(filename)?;
    for line in lines {
        let l = line?;
        match l.as_str() {
            "" => {
                top_calories.add_if_top(elf_calories);
                elf_calories = 0;
                continue;
            },
            _ => {
                let cal = l.parse::<i32>()?;
                elf_calories += cal;
            }
        }
    }

    // handle last elf in case there was no final blank line
    if elf_calories > 0 {
        top_calories.add_if_top(elf_calories);
    }

    Ok(top_calories)
}

// from https://doc.rust-lang.org/rust-by-example/std_misc/file/read_lines.html
fn read_lines<P>(filename: P) -> io::Result<io::Lines<io::BufReader<File>>>
where P: AsRef<Path>, {
    let file = File::open(filename)?;
    Ok(io::BufReader::new(file).lines())
}
