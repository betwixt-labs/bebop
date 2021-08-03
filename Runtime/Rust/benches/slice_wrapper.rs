use criterion::{black_box, criterion_group, Criterion};

fn get_raw() {}

criterion_group!(benches, get_raw);
