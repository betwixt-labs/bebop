use criterion::criterion_main;
mod jazz;

criterion_main!(jazz::benches);
