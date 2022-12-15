use std::fs;
use criterion::{criterion_group, criterion_main, BenchmarkId, Criterion};
use day_03_rucksack_reorganization::{run, run_lines};

pub fn criterion_benchmark(c: &mut Criterion) {
    const FILENAME: &str = "../input.txt";
    let contents = fs::read_to_string(FILENAME).expect("failed to read file");

    c.bench_with_input(
        BenchmarkId::new("day 3", "input.txt"),
        &FILENAME,
        |b, f| b.iter(|| {
            run(f)
        }));

    c.bench_with_input(
        BenchmarkId::new("day 3", "in memory"),
        &contents,
        |b, c| b.iter(|| {
            run_lines(c)
        }));
}

criterion_group!(benches, criterion_benchmark);
criterion_main!(benches);
