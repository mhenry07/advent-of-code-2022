use std::error::Error;
use std::path::Path;
use day_06_tuning_trouble::run;

fn main() -> Result<(), Box<dyn Error>> {
    let filename = Path::new("../input.txt");

    let markers = run(filename)?;
    println!("First markers: {:?}", markers);

    Ok(())
}
