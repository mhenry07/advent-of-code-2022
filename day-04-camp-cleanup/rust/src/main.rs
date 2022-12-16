use std::path::Path;
use std::process;
use day_04_camp_cleanup::run;

fn main() {
    let filename = Path::new("../input.txt");

    match run(filename) {
        Ok(counts) => println!("Pairs with overlap: {:?}", counts),
        Err(e) => {
            println!("Application error: {e}");
            process::exit(1);
        }
    }
}
