use std::error::Error;
use std::path::Path;
use day_07_no_space_left::run;

fn main() -> Result<(), Box<dyn Error>> {
    let filename = Path::new("../input.txt");

    let sizes = run(filename)?;
    println!("Sizes: {:?}", sizes);

    Ok(())
}
