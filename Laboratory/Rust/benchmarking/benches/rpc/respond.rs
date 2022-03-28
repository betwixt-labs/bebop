use std::str::FromStr;

use bebop::prelude::*;
use bebop::rpc::Router;
use criterion::{black_box, Criterion};
use tokio::runtime::Runtime;

use benchmarking::bebops::rpc::owned::SRequests;
use benchmarking::bebops::rpc::ObjA;

use crate::rpc::make_routers;

pub(super) fn bench(c: &mut Criterion, runtime: &Runtime) {
    let (router_a, router_b) = make_routers(runtime);

    respond_a(c, runtime, &router_a);
    respond_b(c, runtime, &router_a);
    respond_c(c, runtime, router_a);

    // this was acting as the server, put this here to prevent unused warnings.
    drop(router_b);
}

fn respond_a(c: &mut Criterion, runtime: &Runtime, router_a: &Router<SRequests>) {
    let mut group = c.benchmark_group("Respond A");
    group.bench_function("Bebop", |b| {
        b.iter(|| {
            runtime.block_on(async {
                let obj = router_a.respond_a(None).await.unwrap();
                assert_eq!(obj.a, -34234);
                assert_eq!(obj.c, Date::from_secs(8497589634))
            })
        })
    });
    group.finish();
}

fn respond_b(c: &mut Criterion, runtime: &Runtime, router_a: &Router<SRequests>) {
    let mut group = c.benchmark_group("Respond B");
    group.bench_function("Bebop", |b| {
        b.iter(|| {
            runtime.block_on(async {
                let obj = router_a.respond_b(None).await.unwrap();
                assert_eq!(obj.a, Some(3));
                assert_eq!(obj.e.as_deref(), Some("s890df7g"));
            })
        })
    });
    group.finish();
}

fn respond_c(c: &mut Criterion, runtime: &Runtime, router_a: Router<SRequests>) {
    let mut group = c.benchmark_group("Respond C");
    group.bench_function("Bebop", |b| {
        b.iter(|| {
            runtime.block_on(async {
                let obj = router_a.respond_c(None).await.unwrap();
                assert_eq!(obj.b, "iogy");
                assert_eq!(obj.c[1], 23.34);
                assert_eq!(obj.a.b, 6);
            })
        })
    });
    group.finish();
}
