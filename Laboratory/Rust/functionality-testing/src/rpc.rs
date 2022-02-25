use std::pin::Pin;
use std::sync::Arc;
use std::time::Instant;

use bebop::prelude::*;
use bebop::rpc::{LocalRpcResponse, TransportHandler, TransportProtocol, TransportResult};
use bebop::timeout;

pub use crate::generated::rpc::owned::NullServiceRequests;
use crate::generated::rpc::owned::{KVStoreHandlersDef, KVStoreRequests, NullServiceHandlers, KV};

struct ChannelTransport {
    handler: Option<Pin<Box<dyn TransportHandler>>>,
}

impl ChannelTransport {
    fn new() -> (Self, Self) {
        (Self { handler: None }, Self { handler: None })
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
        todo!()
    }
}

struct MemBackedKVStore();

impl MemBackedKVStore {
    fn new() -> Arc<Self> {
        Arc::new(Self())
    }
}

// impl for Arc so that we can create weak references that we pass to the futures without capturing
// the lifetime of the self reference.
impl KVStoreHandlersDef for Arc<MemBackedKVStore> {
    fn ping<'f>(&self, deadline: Option<Instant>) -> DynFuture<'f, LocalRpcResponse<()>> {
        todo!()
    }

    fn entries<'f>(
        &self,
        deadline: Option<Instant>,
        page: u64,
        page_size: u16,
    ) -> DynFuture<'f, LocalRpcResponse<Vec<KV>>> {
        todo!()
    }

    fn keys<'f>(
        &self,
        deadline: Option<Instant>,
        page: u64,
        page_size: u16,
    ) -> DynFuture<'f, LocalRpcResponse<Vec<String>>> {
        todo!()
    }

    fn values<'f>(
        &self,
        deadline: Option<Instant>,
        page: u64,
        page_size: u16,
    ) -> DynFuture<'f, LocalRpcResponse<Vec<String>>> {
        todo!()
    }

    fn insert<'f>(
        &self,
        deadline: Option<Instant>,
        key: String,
        value: String,
    ) -> DynFuture<'f, LocalRpcResponse<bool>> {
        todo!()
    }

    fn insert_many<'f>(
        &self,
        deadline: Option<Instant>,
        entries: Vec<KV>,
    ) -> DynFuture<'f, LocalRpcResponse<Vec<String>>> {
        todo!()
    }

    fn get<'f>(
        &self,
        deadline: Option<Instant>,
        key: String,
    ) -> DynFuture<'f, LocalRpcResponse<String>> {
        todo!()
    }

    fn count<'f>(&self, deadline: Option<Instant>) -> DynFuture<'f, LocalRpcResponse<u64>> {
        todo!()
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
