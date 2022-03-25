use bebop::prelude::*;
use bebop::rpc::{
    RequestHeader, ResponseHeader, Router, TransportHandler, TransportProtocol, TransportResult,
};
use bebop::timeout;
use benchmarking::bebops::rpc::owned::{SHandlers, SRequests};
use benchmarking::rpc::Service;
use criterion::{Criterion, Throughput};
use std::sync::Arc;
use tokio::sync::mpsc;
use tokio::{join, select};

mod transport;

struct ChannelTransport {
    handler: Option<TransportHandler>,
    tx: tokio::sync::mpsc::Sender<Vec<u8>>,
}

const CHANNEL_BUFFER_SIZE: usize = 8;

impl ChannelTransport {
    fn make(
        rt: tokio::runtime::Handle,
        tx: mpsc::Sender<Vec<u8>>,
        mut rx: mpsc::Receiver<Vec<u8>>,
    ) -> Arc<Self> {
        let zelf = Arc::new(Self { tx, handler: None });
        let weak = Arc::downgrade(&zelf);
        rt.spawn(async move {
            while let Some(packet) = rx.recv().await {
                if let Some(zelf) = weak.upgrade() {
                    // We spawn here so that requests don't block each other; this would be a good
                    // place to return a "too many requests" error if the server is overloaded
                    // rather than spawning the request.
                    tokio::spawn(async move {
                        let datagram = Datagram::deserialize(&packet).unwrap();
                        debug_assert!(zelf.handler.is_some());
                        let handler = unsafe { zelf.handler.as_ref().unwrap_unchecked() };
                        if let Some(fut) = handler.handle(&datagram) {
                            fut.await;
                        }
                        // not sure why exactly, but we need to explicitly drop datagram for
                        // lifetime compliance reasons.
                        drop(datagram);
                    });
                } else {
                    break;
                }
            }
        });
        zelf
    }

    pub fn new(rt: tokio::runtime::Handle) -> (Arc<Self>, Arc<Self>) {
        let (tx_a, rx_a) = tokio::sync::mpsc::channel(CHANNEL_BUFFER_SIZE);
        let (tx_b, rx_b) = tokio::sync::mpsc::channel(CHANNEL_BUFFER_SIZE);

        let a = Self::make(rt.clone(), tx_a, rx_b);
        let b = Self::make(rt, tx_b, rx_a);

        (a, b)
    }

    async fn recv<'a, 'b: 'a>(&self, datagram: &'a Datagram<'b>) {
        debug_assert!(self.handler.is_some());
        let handler = unsafe { self.handler.as_ref().unwrap_unchecked() };
        if let Some(fut) = handler.handle(&datagram) {
            // awaiting here allows for backpressure on requests, could just spawn instead.
            fut.await
        }
    }
}

impl TransportProtocol for ChannelTransport {
    fn set_handler(&mut self, recv: TransportHandler) {
        self.handler = Some(recv);
    }

    fn send(&self, datagram: &Datagram) -> DynFuture<TransportResult> {
        let tx = self.tx.clone();
        let raw = datagram.serialize_to_vec().unwrap();
        Box::pin(async move {
            if let Err(err) = tx.send(raw).await {
                println!("Warning, channel send error: {err}")
            }
            Ok(())
        })
    }
}

// /// The goal of the hand-rolled service is not to try and test what happens if you remove any
// /// features that are not relevant, but rather to see what performance impact having the generic
// /// datagram has and some of the generic wrapping logic. The goal is therefore to maintain the full
// /// feature set of bebop rpc but to do it without unnecessary generated code or generic buffers.
// struct HandrolledService {
//     rx: tokio::sync::mpsc::Receiver<Vec<u8>>,
//     tx: tokio::sync::mpsc::Sender<Vec<u8>>,
//     /// You would need to build something like this for the async handling, so might as well use the
//     /// same one to remove any potential difference in implementation performance
//     calls: RouterCallTable,
// }
//
// impl HandrolledService {
//     fn new() -> (Arc<HandrolledService>, Arc<HandrolledService>) {
//         let (tx_a, rx_a) = tokio::sync::mpsc::channel(CHANNEL_BUFFER_SIZE);
//         let (tx_b, rx_b) = tokio::sync::mpsc::channel(CHANNEL_BUFFER_SIZE);
//
//         let a = Arc::new(Self { tx: tx_a, rx: rx_b, calls: RouterCallTable::default() });
//         let b = Arc::new(Self { tx: tx_b, rx: rx_a, calls: RouterCallTable::default() });
//         let weak_a = Arc::downgrade(&a);
//         let weak_b = Arc::downgrade(&b);
//
//         tokio::spawn(async move {
//
//         });
//         // unsafe {
//         //     let ptr_a: *mut HandrolledService = a.as_ref() as *const _ as *mut _;
//         //     let ptr_b: *mut HandrolledService = b.as_ref() as *const _ as *mut _;
//         //
//         // }
//
//         (a, b)
//     }
//
//     async fn recv(zelf: Weak<Self>, mut rx: tokio::sync::mpsc::Receiver<Vec<u8>>) {
//         while let Some(data) = rx.recv().await {
//             if let Some(s) = zelf.upgrade() {
//                 s.calls.
//             } else {
//                 // time to stop running
//                 return;
//             }
//         }
//     }
//
//     #[inline(always)]
//     fn send(&self) {
//
//     }
//
//     pub async fn ping(&self) {
//         let d = benchmarking::bebops::rpc::HandrolledDatagram::PingRequest { header: benchmarking::bebops::rpc::HandrolledRequestHeader { id: } };
//         // self.tx.send()
//     }
// }

pub fn run(c: &mut Criterion) {
    let runtime = Arc::new(
        tokio::runtime::Builder::new_multi_thread()
            .enable_all()
            // for benchmarking, want to do single-threaded performance
            .worker_threads(1)
            .build()
            .expect("Failed to create tokio runtime!"),
    );

    transport::bench(c, &runtime);

    let rt = runtime.handle().clone();
    let (transport_a, transport_b) = ChannelTransport::new(rt.clone());

    let router_a = Router::<SRequests>::new(
        transport_a,
        SHandlers::from(Arc::new(Service)),
        Box::pin(move |f| {
            rt.spawn(f);
        }),
        None,
    );
    let rt = runtime.handle().clone();
    let router_b = Router::<SRequests>::new(
        transport_b,
        SHandlers::from(Arc::new(Service)),
        Box::pin(move |f| {
            rt.spawn(f);
        }),
        None,
    );

    let mut group = c.benchmark_group("monodi ping - 2");
    group.sample_size(500);
    group.throughput(Throughput::Elements(2));
    let rt = runtime.handle().clone();
    group.bench_function("Bebop", |b| {
        b.iter(|| {
            rt.block_on(async {
                let (a, b) = join!(router_a.ping(timeout!(2 s)), router_a.ping(timeout!(2 s)));
                a.unwrap();
                b.unwrap();
            });
        })
    });
    group.finish();

    let mut group = c.benchmark_group("bidi ping - 2");
    group.sample_size(500);
    group.throughput(Throughput::Elements(2));
    let rt = runtime.handle().clone();
    group.bench_function("Bebop", |b| {
        b.iter(|| {
            rt.block_on(async {
                let (a, b) = join!(router_a.ping(timeout!(2 s)), router_b.ping(timeout!(2 s)));
                a.unwrap();
                b.unwrap();
            });
        })
    });
    group.finish();

    let mut group = c.benchmark_group("monodi ping - 16");
    group.sample_size(500);
    group.throughput(Throughput::Elements(16));
    let rt = runtime.handle().clone();
    group.bench_function("Bebop", |b| {
        b.iter(|| {
            rt.block_on(async {
                let tup = join!(
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

    let mut group = c.benchmark_group("bidi ping - 16");
    group.sample_size(500);
    group.throughput(Throughput::Elements(16));
    let rt = runtime.handle().clone();
    group.bench_function("Bebop", |b| {
        b.iter(|| {
            rt.block_on(async {
                let tup = join!(
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