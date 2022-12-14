use std::fs;

use criterion::{criterion_group, criterion_main, BenchmarkId, Criterion};
use day_02_rock_paper_scissors::{run, Round};

pub fn criterion_benchmark(c: &mut Criterion) {
    const FILENAME: &str = "../input.txt";
    let contents = fs::read_to_string(FILENAME).expect("failed to read file");

    c.bench_with_input(
        BenchmarkId::new("day 2", "input.txt"),
        &FILENAME,
        |b, f| b.iter(|| {
            run(f)
        }));

    c.bench_with_input(
        BenchmarkId::new("day 2", "in memory"),
        &contents,
        |b, c| b.iter(|| {
            let mut score_1 = 0;
            let mut score_2 = 0;
        
            for line in c.lines() {
                let round = Round::parse(&line).unwrap();
        
                score_1 += round.score_1();
                score_2 += round.score_2();
            }
        
            [score_1, score_2]
        }));
}

criterion_group!(benches, criterion_benchmark);
criterion_main!(benches);
