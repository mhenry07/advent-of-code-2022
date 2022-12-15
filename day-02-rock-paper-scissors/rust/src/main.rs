use std::path::Path;
use std::process;
use day_02_rock_paper_scissors::run;

fn main() {
    let filename = Path::new("../input.txt");

    match run(filename) {
        Ok(scores) => println!("Scores: {:?}", scores),
        Err(e) => {
            println!("Application error: {e}");
            process::exit(1);
        }
    }
}
