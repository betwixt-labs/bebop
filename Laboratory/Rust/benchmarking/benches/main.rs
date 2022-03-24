use criterion::{criterion_main, criterion_group};

mod serialization;
mod rpc;

// criterion_group!(serialization, serialization::run);
criterion_group!(rpc, rpc::run);

// criterion_main!(serialization, rpc);
criterion_main!(rpc);
