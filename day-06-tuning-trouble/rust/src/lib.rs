use std::error::Error;
use std::fmt;
use std::fs::File;
use std::io::{self, BufRead};
use std::path::Path;
use std::str::FromStr;

/// Solves Day 6 from a file path `filename`
pub fn run<P>(filename: P) -> Result<Results, Box<dyn Error>>
where P: AsRef<Path> {
    let lines = read_lines(filename)?;
    for l in lines {
        let line = &l?;
        return Ok(line.parse::<Results>()?);
    }

    Err(Box::new(MessageProcessingError { details: String::from("Message not found") }))
}

/// Solves Day 6 from a string slice `input`
pub fn run_lines(input: &str) -> Result<Results, Box<dyn Error>> {
    for line in input.lines() {
        return Ok(line.parse::<Results>()?);
    }

    Err(Box::new(MessageProcessingError { details: String::from("Message not found") }))
}

#[derive(Debug)]
pub struct Results
{
    packet_marker: i32,
    message_marker: i32
}

impl FromStr for Results {
    type Err = MessageProcessingError;

    fn from_str(s: &str) -> Result<Self, Self::Err> {
        let packet_marker = find_first_marker(s, MarkerType::Packet)
            .ok_or_else(|| MessageProcessingError { details: String::from("Failed to find first packet marker") })?;

        let message_marker = find_first_marker(s, MarkerType::Message)
            .ok_or_else(|| MessageProcessingError { details: String::from("Failed to find first message marker") })?;

        Ok(Results { packet_marker, message_marker })
    }
}

enum MarkerType {
    Packet,
    Message
}

#[derive(Debug)]
pub struct MessageProcessingError {
    details: String
}

impl fmt::Display for MessageProcessingError {
    fn fmt(&self, f: &mut fmt::Formatter) -> fmt::Result {
        write!(f,"{}",self.details)
    }
}

impl Error for MessageProcessingError {
    fn description(&self) -> &str {
        &self.details
    }
}

fn find_first_marker(s: &str, marker_type: MarkerType) -> Option<i32> {
    let n = match marker_type {
        MarkerType::Packet => 4,
        MarkerType::Message => 14
    };

    for i in n-1..s.len() {
        let potential_marker = &s[i+1-n..=i];
        if is_marker(potential_marker) {
            return Some(1 + i as i32)
        }
    }

    None
}

fn is_marker(s: &str) -> bool {
    let bytes = s.as_bytes();
    for i in 0..bytes.len() {
        for j in i+1..bytes.len() {
            if &bytes[i] == &bytes[j] {
                return false;
            }
        }
    }

    true
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

    const EXAMPLE_A: &'static str = "mjqjpqmgbljsphdztnvjfqwrcgsmlb";
    const EXAMPLE_B: &'static str = "bvwbjplbgvbhsrlpgdmjqwftvncz";
    const EXAMPLE_C: &'static str = "nppdvjthqldpwncqszvftbrmjlhg";
    const EXAMPLE_D: &'static str = "nznrnfrfntjfmvfwmzdfjlvtqnbhcprsg";
    const EXAMPLE_E: &'static str = "zcfzfwzzqfrljwzlrfnpqdbhtmscgvjw";

    #[test]
    fn part_1_a() {
        let results = run_lines(EXAMPLE_A).unwrap();

        assert_eq!(results.packet_marker, 7)
    }

    #[test]
    fn part_1_b() {
        let results = run_lines(EXAMPLE_B).unwrap();

        assert_eq!(results.packet_marker, 5)
    }

    #[test]
    fn part_1_c() {
        let results = run_lines(EXAMPLE_C).unwrap();

        assert_eq!(results.packet_marker, 6)
    }

    #[test]
    fn part_1_d() {
        let results = run_lines(EXAMPLE_D).unwrap();

        assert_eq!(results.packet_marker, 10)
    }

    #[test]
    fn part_1_e() {
        let results = run_lines(EXAMPLE_E).unwrap();

        assert_eq!(results.packet_marker, 11)
    }

    #[test]
    fn part_2_a() {
        let results = run_lines(EXAMPLE_A).unwrap();

        assert_eq!(results.message_marker, 19)
    }

    #[test]
    fn part_2_b() {
        let results = run_lines(EXAMPLE_B).unwrap();

        assert_eq!(results.message_marker, 23)
    }

    #[test]
    fn part_2_c() {
        let results = run_lines(EXAMPLE_C).unwrap();

        assert_eq!(results.message_marker, 23)
    }

    #[test]
    fn part_2_d() {
        let results = run_lines(EXAMPLE_D).unwrap();

        assert_eq!(results.message_marker, 29)
    }

    #[test]
    fn part_2_e() {
        let results = run_lines(EXAMPLE_E).unwrap();

        assert_eq!(results.message_marker, 26)
    }
}
