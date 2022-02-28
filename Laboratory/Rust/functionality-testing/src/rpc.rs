use std::collections::hash_map::Entry;
use std::collections::{HashMap, HashSet};
use std::pin::Pin;
use std::sync::Arc;
use std::time::Instant;

use bebop::prelude::*;
use bebop::rpc::{
    LocalRpcError, LocalRpcResponse, Router, TransportHandler, TransportProtocol, TransportResult,
};
use bebop::{dyn_fut, timeout};
// Usually I would use parking lot, but since we are simulating a database I think this will give
// a better impression of what the operations will look like with the required awaits.
use tokio::sync::RwLock;

pub use crate::generated::rpc::owned::NullServiceRequests;
use crate::generated::rpc::owned::{
    KVStoreHandlers, KVStoreHandlersDef, KVStoreRequests, NullServiceHandlers, KV,
};

struct ChannelTransport {
    handler: Option<Pin<Box<dyn TransportHandler>>>,
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
                    let datagram = Datagram::deserialize(&packet).unwrap();
                    zelf.handler.as_deref().unwrap().handle(&datagram);
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

    // TODO: call this function when a datagram is received
    async fn recv<'a, 'b: 'a>(&self, datagram: &'a Datagram<'b>) {
        debug_assert!(self.handler.is_some());
        let handler = unsafe { self.handler.as_deref().unwrap_unchecked() };
        if let Some(fut) = handler.handle(&datagram) {
            // awaiting here allows for backpressure on requests, could just spawn instead.
            fut.await
        }
    }
}

impl TransportProtocol for ChannelTransport {
    fn set_handler(&mut self, recv: Pin<Box<dyn TransportHandler>>) {
        self.handler = Some(recv);
    }

    fn send(&self, datagram: &Datagram) -> DynFuture<TransportResult> {
        let tx = self.tx.clone();
        let raw = datagram.serialize_to_vec().unwrap();
        Box::pin(async move {
            tx.send(raw).await;
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
    fn ping<'f>(&self, _deadline: Option<Instant>) -> DynFuture<'f, LocalRpcResponse<()>> {
        Box::pin(async move { Ok(()) })
    }

    fn entries<'f>(
        &self,
        _deadline: Option<Instant>,
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
        _deadline: Option<Instant>,
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
        _deadline: Option<Instant>,
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
        _deadline: Option<Instant>,
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
        _deadline: Option<Instant>,
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

    fn get<'f>(
        &self,
        _deadline: Option<Instant>,
        key: String,
    ) -> DynFuture<'f, LocalRpcResponse<String>> {
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

    fn count<'f>(&self, _deadline: Option<Instant>) -> DynFuture<'f, LocalRpcResponse<u64>> {
        let zelf = self.clone();
        Box::pin(async move { Ok(zelf.0.read().await.len() as u64) })
    }
}

struct NullServiceHandlersImpl;
impl crate::generated::rpc::owned::NullServiceHandlersDef for NullServiceHandlersImpl {}

#[tokio::test]
async fn main() {
    let kv_store = MemBackedKVStore::new();
    let kv_store_handlers = KVStoreHandlers::from(kv_store);

    let (transport_a, transport_b) = ChannelTransport::new();

    let server = Router::<NullServiceRequests>::new(
        transport_a,
        kv_store_handlers,
        Box::pin(|f| {
            tokio::spawn(f);
        }),
        None,
    );

    let client = Router::<KVStoreRequests>::new(
        transport_b,
        NullServiceHandlers::from(NullServiceHandlersImpl),
        Box::pin(|f| {
            tokio::spawn(f);
        }),
        None,
    );

    client
        .insert(timeout!(1 s), "Mykey", "Myvalue")
        .await
        .unwrap();
    assert_eq!(client.count(timeout!(5 sec)).await.unwrap(), 1);
    assert_eq!(
        &client.get(timeout!(100 ms), "Mykey").await.unwrap(),
        "Myvalue"
    );
}
