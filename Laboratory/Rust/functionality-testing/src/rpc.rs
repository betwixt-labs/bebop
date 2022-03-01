use std::collections::hash_map::Entry;
use std::collections::HashMap;
use std::sync::Arc;
use std::time::{Duration, Instant};

use bebop::prelude::*;
use bebop::rpc::LocalRpcError::DeadlineExceded;
use bebop::rpc::{
    Deadline, LocalRpcError, LocalRpcResponse, TransportError, TransportHandler, TransportProtocol,
    TransportResult,
};
use bebop::rpc::{RemoteRpcError, Router};
use bebop::timeout;
// Usually I would use parking lot, but since we are simulating a database I think this will give
// a better impression of what the operations will look like with the required awaits.
use tokio::sync::RwLock;

pub use crate::generated::rpc::owned::NullServiceRequests;
use crate::generated::rpc::owned::{KVStoreHandlers, KVStoreRequests, NullServiceHandlers};
use crate::generated::rpc::owned::{KVStoreHandlersDef, KV};

struct ChannelTransport {
    handler: Option<TransportHandler>,
    tx: tokio::sync::mpsc::Sender<Vec<u8>>,
}

impl ChannelTransport {
    fn make(
        tx: tokio::sync::mpsc::Sender<Vec<u8>>,
        mut rx: tokio::sync::mpsc::Receiver<Vec<u8>>,
    ) -> Arc<Self> {
        let zelf = Arc::new(Self { tx, handler: None });
        let weak = Arc::downgrade(&zelf);
        tokio::spawn(async move {
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

    pub fn new() -> (Arc<Self>, Arc<Self>) {
        let (tx_a, rx_a) = tokio::sync::mpsc::channel(16);
        let (tx_b, rx_b) = tokio::sync::mpsc::channel(16);

        let a = Self::make(tx_a, rx_b);
        let b = Self::make(tx_b, rx_a);

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

struct MemBackedKVStore(RwLock<HashMap<String, String>>);

impl MemBackedKVStore {
    fn new() -> Arc<Self> {
        Arc::new(Self(RwLock::default()))
    }
}

// impl for Arc so that we can create weak references that we pass to the futures without capturing
// the lifetime of the self reference.
impl KVStoreHandlersDef for Arc<MemBackedKVStore> {
    fn ping<'f>(&self, _deadline: Deadline) -> DynFuture<'f, LocalRpcResponse<()>> {
        Box::pin(async move { Err(LocalRpcError::CustomErrorStatic(4, "some error")) })
    }

    fn entries<'f>(
        &self,
        _deadline: Deadline,
        page: u64,
        page_size: u16,
    ) -> DynFuture<'f, LocalRpcResponse<Vec<KV>>> {
        // NOTE: it is not valid to capture `self` in the future! This is enforced by lifetime
        // constraints.

        let zelf = self.clone();
        Box::pin(async move {
            Ok(zelf
                .0
                .read()
                .await
                .iter()
                .skip(page as usize * page_size as usize)
                .take(page_size as usize)
                .map(|(k, v)| KV {
                    key: k.clone(),
                    value: v.clone(),
                })
                .collect())
        })
    }

    fn keys<'f>(
        &self,
        _deadline: Deadline,
        page: u64,
        page_size: u16,
    ) -> DynFuture<'f, LocalRpcResponse<Vec<String>>> {
        let zelf = self.clone();
        Box::pin(async move {
            Ok(zelf
                .0
                .read()
                .await
                .keys()
                .skip(page as usize * page_size as usize)
                .take(page_size as usize)
                .cloned()
                .collect())
        })
    }

    fn values<'f>(
        &self,
        _deadline: Deadline,
        page: u64,
        page_size: u16,
    ) -> DynFuture<'f, LocalRpcResponse<Vec<String>>> {
        let zelf = self.clone();
        Box::pin(async move {
            Ok(zelf
                .0
                .read()
                .await
                .values()
                .skip(page as usize * page_size as usize)
                .take(page_size as usize)
                .cloned()
                .collect())
        })
    }

    fn insert<'f>(
        &self,
        _deadline: Deadline,
        key: String,
        value: String,
    ) -> DynFuture<'f, LocalRpcResponse<bool>> {
        let zelf = self.clone();
        Box::pin(async move {
            Ok(match zelf.0.write().await.entry(key) {
                Entry::Occupied(_) => false,
                Entry::Vacant(entry) => {
                    entry.insert(value);
                    true
                }
            })
        })
    }

    fn insert_many<'f>(
        &self,
        _deadline: Deadline,
        entries: Vec<KV>,
    ) -> DynFuture<'f, LocalRpcResponse<Vec<String>>> {
        let zelf = self.clone();
        Box::pin(async move {
            let mut lock = zelf.0.write().await;
            let preexisting = entries
                .into_iter()
                .filter_map(|KV { key, value }| match lock.entry(key) {
                    Entry::Occupied(entry) => Some(entry.key().clone()),
                    Entry::Vacant(entry) => {
                        entry.insert(value);
                        None
                    }
                })
                .collect();
            Ok(preexisting)
        })
    }

    fn get<'f>(&self, _deadline: Deadline, key: String) -> DynFuture<'f, LocalRpcResponse<String>> {
        let zelf = self.clone();
        Box::pin(async move {
            zelf.0
                .read()
                .await
                .get(&key)
                .cloned()
                .ok_or(LocalRpcError::CustomErrorStatic(1, "Unknown key"))
        })
    }

    fn count<'f>(&self, _deadline: Deadline) -> DynFuture<'f, LocalRpcResponse<u64>> {
        let zelf = self.clone();
        Box::pin(async move { Ok(zelf.0.read().await.len() as u64) })
    }

    fn wait<'f>(&self, deadline: Deadline, secs: u16) -> DynFuture<'f, LocalRpcResponse<()>> {
        let zelf = self.clone();
        Box::pin(async move {
            if let Some(deadline) = deadline.as_ref() {
                if *deadline - Instant::now() >= Duration::from_secs(secs as u64) {
                    // we will eventually time out, so just do it now
                    return Err(DeadlineExceded);
                }
            }
            let _lock = zelf.0.write().await;
            tokio::time::sleep(Duration::from_secs(secs as u64)).await;
            if deadline.has_passed() {
                Err(DeadlineExceded)
            } else {
                Ok(())
            }
        })
    }
}

struct NullServiceHandlersImpl;
impl crate::generated::rpc::owned::NullServiceHandlersDef for NullServiceHandlersImpl {}

fn setup() -> (Router<NullServiceRequests>, Router<KVStoreRequests>) {
    let kv_store = MemBackedKVStore::new();
    let kv_store_handlers = KVStoreHandlers::from(kv_store);
    let (transport_a, transport_b) = ChannelTransport::new();
    let runtime = tokio::runtime::Handle::current();
    (
        Router::new(
            transport_a,
            kv_store_handlers,
            Box::pin(move |f| {
                runtime.spawn(f);
            }),
            None,
        ),
        Router::new(
            transport_b,
            NullServiceHandlers::from(NullServiceHandlersImpl),
            Box::pin(|f| {
                tokio::spawn(f);
            }),
            None,
        ),
    )
}

#[tokio::test(flavor = "multi_thread", worker_threads = 2)]
async fn can_request_and_receive() {
    let (_server, client) = setup();
    client
        .insert(timeout!(1 s), "Mykey", "Myvalue")
        .await
        .unwrap();
    assert_eq!(client.count(timeout!(5 sec)).await.unwrap(), 1);
    assert_eq!(
        &client.get(timeout!(10 s), "Mykey").await.unwrap(),
        "Myvalue"
    );
}

#[tokio::test(flavor = "multi_thread", worker_threads = 2)]
async fn handles_custom_errors() {
    let (_server, client) = setup();
    let res = client.ping(timeout!(None)).await;
    assert!(matches!(res, Err(RemoteRpcError::CustomError(4, Some(_)))));
}

#[tokio::test(flavor = "multi_thread", worker_threads = 2)]
async fn handles_remote_timeout_error() {
    let (_server, client) = setup();
    let start = Instant::now();
    let res = client.wait(timeout!(1 s), 10).await;
    assert!(Instant::now() - start < Duration::from_millis(1100));
    assert!(matches!(
        res,
        Err(RemoteRpcError::TransportError(TransportError::Timeout))
    ));
}

static_assertions::assert_impl_all!(Router<KVStoreRequests>: Send, Sync, Clone);

#[tokio::test(flavor = "multi_thread", worker_threads = 2)]
async fn handles_local_timeout_error() {
    let (_server, client) = setup();
    let start = Instant::now();

    let wait_fut = client.wait(None, 2);
    let count_fut = async {
        // make sure the wait future always is processed first
        tokio::time::sleep(Duration::from_millis(10)).await;
        client.count(timeout!(1 s)).await
    };
    let res = tokio::select! {
        res1 = wait_fut => { res1.unwrap(); None },
        res2 = count_fut => Some(res2),
    };

    let res = res.expect("Count should have completed first");
    assert!(Instant::now() - start < Duration::from_millis(1100));
    assert!(matches!(
        res,
        Err(RemoteRpcError::TransportError(TransportError::Timeout))
    ));
    // make sure that when we get the message back (since it does not check if past the deadline) it
    // does not cause any errors.
    tokio::time::sleep(Duration::from_secs(2)).await;
}
