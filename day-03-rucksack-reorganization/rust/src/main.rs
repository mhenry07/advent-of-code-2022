use std::error::Error;
use std::path::Path;
use day_03_rucksack_reorganization::run;

fn main() -> Result<(), Box<dyn Error>> {
    let filename = Path::new("../input.txt");

    let scores = run(filename)?;
    println!("Sum of priorities: {:?}", scores);

    Ok(())
}
