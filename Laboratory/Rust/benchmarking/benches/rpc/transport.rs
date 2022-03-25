use super::CHANNEL_BUFFER_SIZE;
use bebop::prelude::*;
use bebop::rpc::ResponseHeader;
use benchmarking::bebops::rpc::ObjB;
use criterion::{black_box, BenchmarkId, Criterion, Throughput};
use std::sync::Arc;
use bebop::SeResult;
use serde::Serialize;
use tokio::runtime::Runtime;

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

trait BebopSerializeable {
    fn serialize(&self, dest: &mut [u8]) -> SeResult<usize>;
}

impl<R: Record> BebopSerializeable for R {
    
    #[inline(always)]
    fn serialize(&self, dest: &mut [u8]) -> SeResult<usize> {
        (self as Record).serialize(dest)
    }
} 

/// Get a baseline of the transport + serialization speed. This does use the datagram structure
/// and does double-deserialize, however, it does it as we would expect any normal implementation to
/// do it with the datagram not being copied on deserialize and then the inner data being copied
/// into an owned type.
fn transport_datagram(c: &mut Criterion, runtime: &Arc<Runtime>) {
    let mut group = c.benchmark_group("transport - datagram 10000");
    group.throughput(Throughput::Elements(10000));
    let rt = runtime.handle().clone();
    let (tx, mut rx) = tokio::sync::mpsc::channel::<Vec<u8>>(CHANNEL_BUFFER_SIZE);

    let obj_b = ObjB {
        a: Some(2343),
        b: None,
        c: Some(bebop::Date::from_secs_since_unix_epoch(84672397865)),
        d: None,
        e: Some("Hello world!"),
    };
    
    for (param_string, obj) in  {
        group.bench_with_input(BenchmarkId::new("Tokio Channels", param_string), obj, )
    }

    // group.bench_function("Tokio Channels", |b| {
    //     b.iter(|| {
    //         rt.block_on(async {
    //             // create two async tasks, one that produces, and one that consumes.
    //             tokio::join!(
    //                 async {
    //                     let mut buf = Vec::new();
    //                     for i in 0..10000 {
    //                         buf.clear();
    //                         (black_box(ObjB {
    //                             a: Some(2343),
    //                             b: None,
    //                             c: Some(bebop::Date::from_secs_since_unix_epoch(84672397865)),
    //                             d: None,
    //                             e: Some("Hello world!"),
    //                         }))
    //                         .serialize(&mut buf)
    //                         .unwrap();
    //
    //                         let d = black_box(bebop::rpc::Datagram::RpcResponseOk {
    //                             header: ResponseHeader { id: i },
    //                             data: SliceWrapper::Cooked(&buf),
    //                         });
    //                         tx.send(d.serialize_to_vec().unwrap()).await.unwrap();
    //                     }
    //                 },
    //                 async {
    //                     for i in 0..10000 {
    //                         let raw = rx.recv().await.unwrap();
    //                         let datagram = Datagram::deserialize(&raw).unwrap();
    //                         match datagram {
    //                             Datagram::RpcResponseOk { header, data } => {
    //                                 assert_eq!(header.id, i);
    //                                 let o =
    //                                     benchmarking::bebops::rpc::owned::ObjB::deserialize(&data)
    //                                         .unwrap();
    //                                 assert_eq!(o.a, Some(2343))
    //                             }
    //                             _ => unreachable!(),
    //                         }
    //                     }
    //                 }
    //             );
    //         });
    //     })
    // });
    group.finish();
}
