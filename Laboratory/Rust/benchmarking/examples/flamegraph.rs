use bebop::Record;
use criterion::criterion_main;
use criterion::{black_box, criterion_group, Criterion};
use pprof::criterion::{Output, PProfProfiler};

use benchmarking::bebops::jazz as bb;
use benchmarking::native::jazz as na;

const ALLOC_SIZE: usize = 1024 * 1024 * 4;

fn bench(c: &mut Criterion) {
    let mut buf = Vec::with_capacity(ALLOC_SIZE);
    let native_struct = na::make_library();
    let bebop_struct = bb::Library::from(&native_struct);

    c.bench_function("serialize", |b| {
        b.iter(|| {
            bebop_struct.serialize(&mut buf).unwrap();
            assert!(buf.len() <= ALLOC_SIZE);
            buf.clear();
        })
    });

    bebop_struct.serialize(&mut buf).unwrap();
    c.bench_function("deserialize", |b| {
        b.iter(|| {
            bb::Library::deserialize(black_box(&buf)).unwrap();
        })
    });
}

criterion_group! {
    name = benches;
    config = Criterion::default().with_profiler(PProfProfiler::new(100, Output::Flamegraph(None)));
    targets = bench
}

criterion_main!(benches);
