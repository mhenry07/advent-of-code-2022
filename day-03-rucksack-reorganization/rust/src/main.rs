use std::path::Path;
use std::process;
use day_03_rucksack_reorganization::run;

fn main() {
    let filename = Path::new("../input.txt");

    match run(filename) {
        Ok(scores) => println!("Sum of priorities: {:?}", scores),
        Err(e) => {
            println!("Application error: {e}");
            process::exit(1);
        }
    }
}
