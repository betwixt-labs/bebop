use criterion::{criterion_group, criterion_main};

mod rpc;
mod serialization;

// criterion_group!(serialization, serialization::run);
criterion_group!(rpc, rpc::run);

// criterion_main!(serialization, rpc);
criterion_main!(rpc);
