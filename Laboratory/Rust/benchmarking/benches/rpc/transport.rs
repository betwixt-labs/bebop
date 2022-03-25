use std::str::FromStr;
use std::sync::Arc;

use bebop::prelude::*;
use bebop::rpc::ResponseHeader;
use criterion::{black_box, BenchmarkId, Criterion, Throughput};
use tokio::runtime::Runtime;

use benchmarking::bebops::rpc::{ObjA, ObjB, ObjC};

use super::CHANNEL_BUFFER_SIZE;

pub fn bench(c: &mut Criterion, runtime: &Arc<Runtime>) {
    transport_u32(c, runtime);
    transport_datagram(c, runtime);
}

/// let's benchmark the transport to see what the theoretical max speed is on this system without
/// any serialization to get in the way.
fn transport_u32(c: &mut Criterion, runtime: &Arc<Runtime>) {
    let mut group = c.benchmark_group("transport - u32 1");
    group.sample_size(500);
    group.throughput(Throughput::Elements(1));
    let rt = runtime.handle().clone();
    let (tx, mut rx) = tokio::sync::mpsc::channel::<u32>(CHANNEL_BUFFER_SIZE);
    group.bench_function("Tokio Channels", |b| {
        let mut i = 0;
        b.iter(|| {
            rt.block_on(async {
                let v = tokio::join!(tx.send(black_box(i)), rx.recv());
                v.0.unwrap();
                assert_eq!(v.1.unwrap(), i);
            });
            i += 1;
        })
    });
    group.finish();

    let mut group = c.benchmark_group("transport - u32 10000");
    group.throughput(Throughput::Elements(10000));
    let rt = runtime.handle().clone();
    let (tx, mut rx) = tokio::sync::mpsc::channel::<u32>(CHANNEL_BUFFER_SIZE);
    group.bench_function("Tokio Channels", |b| {
        b.iter(|| {
            rt.block_on(async {
                // create two async tasks, one that produces, and one that consumes.
                tokio::join!(
                    async {
                        for i in 0..10000 {
                            tx.send(black_box(i)).await.unwrap();
                        }
                    },
                    async {
                        for i in 0..10000 {
                            assert_eq!(rx.recv().await, Some(i));
                        }
                    }
                );
            });
        })
    });
    group.finish();
}

/// Get a baseline of the transport + serialization speed. This does use the datagram structure
/// and does double-deserialize, however, it does it as we would expect any normal implementation to
/// do it with the datagram not being copied on deserialize and then the inner data being copied
/// into an owned type.
fn transport_datagram(c: &mut Criterion, runtime: &Arc<Runtime>) {
    let mut group = c.benchmark_group("transport - datagram 10000");
    const ELEMENTS: u64 = 10000;
    group.throughput(Throughput::Elements(ELEMENTS));
    let rt = runtime.handle().clone();
    let (tx, mut rx) = tokio::sync::mpsc::channel::<Vec<u8>>(CHANNEL_BUFFER_SIZE);

    let obj_a = ObjA {
        a: 7389347,
        b: 5782,
        c: Date::from_secs(82374532),
        d: Guid::from_str("96c48cd4-5754-4002-afb9-a4a6b7890dcb").unwrap(),
    };

    let obj_b = ObjB {
        a: Some(2343),
        b: None,
        c: Some(bebop::Date::from_secs_since_unix_epoch(84672397865)),
        d: None,
        e: Some("Hello world!"),
    };

    let obj_c = ObjC {
        a: obj_a,
        b: "some message I would assume",
        c: SliceWrapper::from_raw(&[
            21, 41, 43, 12, 5, 54, 6, 2, 73, 24, 76, 12, 23, 5, 3, 0, 0, 0, 0, 0,
        ]),
    };

    let inputs: [(&str, &dyn bebop::Serializable, Box<dyn Fn(&[u8])>); 3] = [
        (
            "ObjA",
            &obj_a,
            Box::new(|buf| {
                assert_eq!(
                    benchmarking::bebops::rpc::owned::ObjA::deserialize(buf)
                        .unwrap()
                        .a,
                    7389347
                );
            }),
        ),
        (
            "ObjB",
            &obj_b,
            Box::new(|buf| {
                assert_eq!(
                    benchmarking::bebops::rpc::owned::ObjB::deserialize(buf)
                        .unwrap()
                        .a,
                    Some(2343)
                );
            }),
        ),
        (
            "ObjC",
            &obj_c,
            Box::new(|buf| {
                assert_eq!(
                    benchmarking::bebops::rpc::owned::ObjC::deserialize(buf)
                        .unwrap()
                        .a
                        .b,
                    5782
                );
            }),
        ),
    ];

    for (param_string, obj, check) in inputs {
        group.bench_with_input(
            BenchmarkId::new("Tokio Channels", param_string),
            &(obj, check),
            |b, (obj, check)| {
                let mut buf = Vec::new();
                b.iter(|| {
                    rt.block_on(async {
                        tokio::join!(
                            async {
                                for i in 0..ELEMENTS {
                                    buf.clear();
                                    obj.serialize(&mut buf).unwrap();
                                    let d = black_box(bebop::rpc::Datagram::RpcResponseOk {
                                        header: ResponseHeader { id: i as u16 },
                                        data: SliceWrapper::Cooked(&buf),
                                    });
                                    tx.send(d.serialize_to_vec().unwrap()).await.unwrap();
                                }
                            },
                            async {
                                for i in 0..ELEMENTS {
                                    let raw = rx.recv().await.unwrap();
                                    let datagram = Datagram::deserialize(&raw).unwrap();
                                    match datagram {
                                        Datagram::RpcResponseOk { header, data } => {
                                            assert_eq!(header.id, i as u16);
                                            check(&data);
                                        }
                                        _ => unreachable!(),
                                    }
                                }
                            }
                        )
                    });
                })
            },
        );
    }
    group.finish();
}
