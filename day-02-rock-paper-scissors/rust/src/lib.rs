use std::{fs::File, io::{self, BufRead}, path::Path};

pub struct Round {
    opponent: Shape,
    unknown: Unknown
}

impl Round {
    pub fn parse(s: &str) -> Option<Round> {
        let tokens = s.split_ascii_whitespace();
        let mut opponent: Option<Shape> = None;
        let mut unknown: Option<Unknown> = None;
        let mut i = 0;
        for token in tokens {
            match i {
                0 => opponent = match token {
                    "A" => Some(Shape::Rock),
                    "B" => Some(Shape::Paper),
                    "C" => Some(Shape::Scissors),
                    _ => return None
                },
                1 => unknown = match token {
                    "X" => Some(Unknown::X),
                    "Y" => Some(Unknown::Y),
                    "Z" => Some(Unknown::Z),
                    _ => return None
                },
                _ => return None
            }
            i += 1;
        }

        match i {
            2 => Some(Round { opponent: opponent?, unknown: unknown? }),
            _ => None
        }
    }

    pub fn score_1(&self) -> i32 {
        let response = match self.unknown {
            Unknown::X => Shape::Rock,
            Unknown::Y => Shape::Paper,
            Unknown::Z => Shape::Scissors
        };

        let outcome = response.outcome(&self.opponent);
        response.score_shape() + outcome.score()
    }

    pub fn score_2(&self) -> i32 {
        let outcome = match self.unknown {
            Unknown::X => Outcome::Lose,
            Unknown::Y => Outcome::Draw,
            Unknown::Z => Outcome::Win
        };

        let response = outcome.response(&self.opponent);
        response.score_shape() + outcome.score()
    }
}

#[derive(Clone, PartialEq, Eq)]
enum Shape {
    Rock,
    Paper,
    Scissors
}

impl Shape {
    fn score_shape(&self) -> i32 {
        match self {
            Shape::Rock => 1,
            Shape::Paper => 2,
            Shape::Scissors => 3
        }
    }

    fn outcome(&self, opponent: &Shape) -> Outcome {
        match (self, opponent) {
            (Shape::Rock, Shape::Scissors)
            | (Shape::Paper, Shape::Rock)
            | (Shape::Scissors, Shape::Paper) => Outcome::Win,
            (Shape::Rock, Shape::Paper)
            | (Shape::Paper, Shape::Scissors)
            | (Shape::Scissors, Shape::Rock) => Outcome::Lose,
            //(s, o) if s == o => Outcome::Draw,
            _ => Outcome::Draw
        }
    }
}

enum Unknown {
    X,
    Y,
    Z
}

enum Outcome {
    Win,
    Lose,
    Draw
}

impl Outcome {
    fn score(&self) -> i32 {
        match self {
            Outcome::Win => 6,
            Outcome::Draw => 3,
            Outcome::Lose => 0
        }
    }

    fn response(&self, opponent: &Shape) -> Shape {
        match (self, opponent) {
            (Outcome::Win, Shape::Rock) => Shape::Paper,
            (Outcome::Win, Shape::Paper) => Shape::Scissors,
            (Outcome::Win, Shape::Scissors) => Shape::Rock,
            (Outcome::Draw, o) => o.clone(),
            (Outcome::Lose, Shape::Rock) => Shape::Scissors,
            (Outcome::Lose, Shape::Paper) => Shape::Rock,
            (Outcome::Lose, Shape::Scissors) => Shape::Paper
        }
    }
}

// from https://doc.rust-lang.org/rust-by-example/std_misc/file/read_lines.html
pub fn read_lines<P>(filename: P) -> io::Result<io::Lines<io::BufReader<File>>>
where P: AsRef<Path>, {
    let file = File::open(filename)?;
    Ok(io::BufReader::new(file).lines())
}
