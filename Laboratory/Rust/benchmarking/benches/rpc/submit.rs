use crate::rpc::make_routers;
use bebop::{Date, Guid, SliceWrapper};
use benchmarking::bebops::rpc::{ObjA, ObjB, ObjC};
use criterion::{black_box, Criterion};
use std::str::FromStr;
use bebop::rpc::Router;
use tokio::runtime::Runtime;
use benchmarking::bebops::rpc::owned::SRequests;

pub(super) fn bench(c: &mut Criterion, runtime: &Runtime) {
    let (router_a, router_b) = make_routers(runtime);

    submit_a(c, runtime, &router_a);
    submit_b(c, runtime, &router_a);
    submit_a_b(c, runtime, &router_a);
    submit_c(c, runtime, &router_a);

    // this was acting as the server, put this here to prevent unused warnings.
    drop(router_b);
}

fn submit_a(c: &mut Criterion, runtime: &Runtime, router_a: &Router<SRequests>) {
    let mut group = c.benchmark_group("Submit A");
    group.bench_function("Bebop", |b| {
        let obj = ObjA {
            a: -34234,
            b: 695,
            c: Date::from_secs(8497589634),
            d: Guid::from_str("f7df0b17-6fc3-4a09-b8c2-308d06e3e558").unwrap(),
        };
        b.iter(|| {
            runtime.block_on(async {
                router_a.submit_a(None, black_box(obj)).await.unwrap();
            })
        })
    });
    group.finish();
}

fn submit_b(c: &mut Criterion, runtime: &Runtime, router_a: &Router<SRequests>) {
    let mut group = c.benchmark_group("Submit B");
    group.bench_with_input(
        "Bebop",
        &ObjB {
            a: Some(3),
            b: Some(5743),
            c: Some(Date::from_secs(9754234)),
            d: Some(Guid::from_str("7b5de104-5a40-47e6-b5d3-ab33f8c82fd2").unwrap()),
            e: Some("s890df7g"),
        },
        |b, obj| {
            b.iter(|| {
                runtime.block_on(async {
                    router_a.submit_b(None, obj.clone()).await.unwrap();
                })
            })
        },
    );
    group.finish();
}

fn submit_a_b(c: &mut Criterion, runtime: &Runtime, router_a: &Router<SRequests>) {
    let mut group = c.benchmark_group("Submit AB");
    group.bench_with_input(
        "Bebop",
        &(
            ObjA {
                a: 97432,
                b: -579,
                c: Date::from_secs(2364),
                d: Guid::from_str("56439f85-8f74-4239-94c2-405706cd8884").unwrap(),
            },
            ObjB {
                a: Some(345),
                b: Some(9543),
                c: Some(Date::from_secs(438751)),
                d: Some(Guid::from_str("cf19c7d9-b662-40fe-a06f-6b57e80cde8f").unwrap()),
                e: Some("gfkhdl"),
            },
        ),
        |b, (obj_a, obj_b)| {
            b.iter(|| {
                runtime.block_on(async {
                    router_a
                        .submit_a_b(
                            None,
                            *black_box(obj_a),
                            black_box(obj_b).clone(),
                            black_box(true),
                            black_box("08fdg"),
                        )
                        .await
                        .unwrap();
                })
            })
        },
    );
    group.finish();
}

fn submit_c(c: &mut Criterion, runtime: &Runtime, router_a: &Router<SRequests>) {
    let mut group = c.benchmark_group("Submit C");
    group.bench_with_input(
        "Bebop",
        &ObjC {
            a: ObjA {
                a: 324,
                b: 6,
                c: Date::from_secs(9864234),
                d: Guid::from_str("2265afc4-7e12-4174-a17c-ee75c4dff074").unwrap()
            },
            b: "iogy",
            c: SliceWrapper::Cooked(&[234.4, 23.34, 54., 543.1])
        },
        |b, obj| {
            b.iter(|| {
                runtime.block_on(async {
                    router_a
                        .submit_c(
                            None,
                            black_box(1234),
                            black_box(obj).clone(),
                        )
                        .await
                        .unwrap();
                })
            })
        },
    );
    group.finish();
}
