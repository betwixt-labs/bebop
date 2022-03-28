use bebop::rpc::Router;
use bebop::timeout;
use benchmarking::bebops::rpc::owned::SRequests;
use criterion::{Criterion, Throughput};
use tokio::runtime::Runtime;

use super::make_routers;

pub(super) fn bench(c: &mut Criterion, runtime: &Runtime) {
    let (router_a, router_b) = make_routers(runtime);

    monodi_2(c, runtime, &router_a);
    bidi_2(c, runtime, &router_a, &router_b);
    monodi_16(c, runtime, &router_a);
    bidi_16(c, runtime, &router_a, &router_b);
}

fn bidi_2(
    c: &mut Criterion,
    runtime: &Runtime,
    router_a: &Router<SRequests>,
    router_b: &Router<SRequests>,
) {
    let mut group = c.benchmark_group("bidi ping - 2");
    group.sample_size(500);
    group.throughput(Throughput::Elements(2));
    let rt = runtime.handle().clone();
    group.bench_function("Bebop", |b| {
        b.iter(|| {
            rt.block_on(async {
                let (a, b) =
                    tokio::join!(router_a.ping(timeout!(2 s)), router_b.ping(timeout!(2 s)));
                a.unwrap();
                b.unwrap();
            });
        })
    });
    group.finish();
}

fn monodi_2(c: &mut Criterion, runtime: &Runtime, router_a: &Router<SRequests>) {
    let mut group = c.benchmark_group("monodi ping - 2");
    group.sample_size(500);
    group.throughput(Throughput::Elements(2));
    let rt = runtime.handle().clone();
    group.bench_function("Bebop", |b| {
        b.iter(|| {
            rt.block_on(async {
                let (a, b) =
                    tokio::join!(router_a.ping(timeout!(2 s)), router_a.ping(timeout!(2 s)));
                a.unwrap();
                b.unwrap();
            });
        })
    });
    group.finish();
}

fn monodi_16(c: &mut Criterion, runtime: &Runtime, router_a: &Router<SRequests>) {
    let mut group = c.benchmark_group("monodi ping - 16");
    group.sample_size(500);
    group.throughput(Throughput::Elements(16));
    let rt = runtime.handle().clone();
    group.bench_function("Bebop", |b| {
        b.iter(|| {
            rt.block_on(async {
                let tup = tokio::join!(
                    router_a.ping(timeout!(2 s)),
                    router_a.ping(timeout!(2 s)),
                    router_a.ping(timeout!(2 s)),
                    router_a.ping(timeout!(2 s)),
                    router_a.ping(timeout!(2 s)),
                    router_a.ping(timeout!(2 s)),
                    router_a.ping(timeout!(2 s)),
                    router_a.ping(timeout!(2 s)),
                    router_a.ping(timeout!(2 s)),
                    router_a.ping(timeout!(2 s)),
                    router_a.ping(timeout!(2 s)),
                    router_a.ping(timeout!(2 s)),
                    router_a.ping(timeout!(2 s)),
                    router_a.ping(timeout!(2 s)),
                    router_a.ping(timeout!(2 s)),
                    router_a.ping(timeout!(2 s)),
                );
                tup.0.unwrap();
                tup.1.unwrap();
                tup.2.unwrap();
                tup.3.unwrap();
                tup.4.unwrap();
                tup.5.unwrap();
                tup.6.unwrap();
                tup.7.unwrap();
                tup.8.unwrap();
                tup.9.unwrap();
                tup.10.unwrap();
                tup.11.unwrap();
                tup.12.unwrap();
                tup.13.unwrap();
                tup.14.unwrap();
                tup.15.unwrap();
            });
        })
    });
    group.finish();
}

fn bidi_16(
    c: &mut Criterion,
    runtime: &Runtime,
    router_a: &Router<SRequests>,
    router_b: &Router<SRequests>,
) {
    let mut group = c.benchmark_group("bidi ping - 16");
    group.sample_size(500);
    group.throughput(Throughput::Elements(16));
    let rt = runtime.handle().clone();
    group.bench_function("Bebop", |b| {
        b.iter(|| {
            rt.block_on(async {
                let tup = tokio::join!(
                    router_a.ping(timeout!(2 s)),
                    router_b.ping(timeout!(2 s)),
                    router_a.ping(timeout!(2 s)),
                    router_b.ping(timeout!(2 s)),
                    router_a.ping(timeout!(2 s)),
                    router_b.ping(timeout!(2 s)),
                    router_a.ping(timeout!(2 s)),
                    router_b.ping(timeout!(2 s)),
                    router_a.ping(timeout!(2 s)),
                    router_b.ping(timeout!(2 s)),
                    router_a.ping(timeout!(2 s)),
                    router_b.ping(timeout!(2 s)),
                    router_a.ping(timeout!(2 s)),
                    router_b.ping(timeout!(2 s)),
                    router_a.ping(timeout!(2 s)),
                    router_b.ping(timeout!(2 s)),
                );
                tup.0.unwrap();
                tup.1.unwrap();
                tup.2.unwrap();
                tup.3.unwrap();
                tup.4.unwrap();
                tup.5.unwrap();
                tup.6.unwrap();
                tup.7.unwrap();
                tup.8.unwrap();
                tup.9.unwrap();
                tup.10.unwrap();
                tup.11.unwrap();
                tup.12.unwrap();
                tup.13.unwrap();
                tup.14.unwrap();
                tup.15.unwrap();
            });
        })
    });
    group.finish();
}
