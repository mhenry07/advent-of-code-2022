use std::error::Error;
use std::path::Path;
use day_02_rock_paper_scissors::run;

fn main() -> Result<(), Box<dyn Error>> {
    let filename = Path::new("../input.txt");

    let scores = run(filename)?;
    println!("Scores: {:?}", scores);

    Ok(())
}
