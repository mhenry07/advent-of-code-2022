use std::path::Path;
use std::process;
use day_06_tuning_trouble::run;

fn main() {
    let filename = Path::new("../input.txt");

    match run(filename) {
        Ok(markers) => println!("First markers: {:?}", markers),
        Err(e) => {
            println!("Application error: {e}");
            process::exit(1);
        }
    }
}
