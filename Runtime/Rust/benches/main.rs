use criterion::criterion_main;

mod serialization;
mod slice_wrapper;

criterion_main!(serialization::benches, slice_wrapper::benches);
