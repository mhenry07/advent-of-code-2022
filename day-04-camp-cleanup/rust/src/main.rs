use std::error::Error;
use std::path::Path;
use day_04_camp_cleanup::run;

fn main() -> Result<(), Box<dyn Error>> {
    let filename = Path::new("../input.txt");

    let counts = run(filename)?;
    println!("Pairs with overlap: {:?}", counts);

    Ok(())
}
