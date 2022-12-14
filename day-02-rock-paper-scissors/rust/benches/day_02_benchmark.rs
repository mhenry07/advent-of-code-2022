use std::fs;

use criterion::{criterion_group, criterion_main, BenchmarkId, Criterion};
use day_02_rock_paper_scissors::{run, Results};

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
            let mut results = Results::new();
            for line in c.lines() {
                results.handle_line(&line).expect("failed to handle line");
            }
            results
        }));
}

criterion_group!(benches, criterion_benchmark);
criterion_main!(benches);
