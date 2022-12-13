use std::error::Error;
use std::path::Path;
use std::process;

use day_02_rock_paper_scissors::{Round, read_lines};

fn main() {
    let filename = "../input.txt";

    match run(filename) {
        Ok(scores) => println!("Scores: {:?}", scores),
        Err(e) => {
            println!("Application error: {e}");
            process::exit(1);
        }
    }
}

fn run<P>(filename: P) -> Result<[i32; 2], Box<dyn Error>>
where P: AsRef<Path> {
    let mut score_1 = 0;
    let mut score_2 = 0;
    let lines = read_lines(filename)?;

    for l in lines {
        let line = l?;
        let round = Round::parse(&line).ok_or_else(|| format!("Error parsing line {line}"))?;

        score_1 += round.score_1();
        score_2 += round.score_2();
    }

    Ok([score_1, score_2])
}
