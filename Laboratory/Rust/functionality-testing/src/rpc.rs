use std::future::Future;
use std::pin::Pin;
use std::sync::Arc;

use async_trait::async_trait;
use bebop::prelude::*;
use bebop::rpc::{
    LocalRpcResponse, Router, ServiceHandlers, TransportHandler, TransportProtocol, TransportResult,
};
use parking_lot::RwLock;
use tokio::sync::mpsc::Sender;

pub use crate::generated::rpc::owned::NullServiceRequests;
use crate::generated::rpc::owned::{KVStoreHandlers, KVStoreRequests, _DynFut, KV};

struct ChannelTransport {
    handler: RwLock<Option<TransportHandler>>,
}

impl ChannelTransport {
    // TODO: call this function when a datagram is received
    async fn recv(&self, datagram: OwnedDatagram) {
        if let Some(fut) = self.handler.read().unwrap()(datagram) {
            // awaiting here allows for backpressure on requests, could just spawn instead.
            fut.await
        }
    }
}

// TODO: remove async_trait requirement?
#[async_trait]
impl TransportProtocol for ChannelTransport {
    fn set_handler(&self, recv: TransportHandler) {
        self.handler.write().insert(recv);
    }

    async fn send(&self, datagram: &Datagram) -> TransportResult {
        todo!()
    }
}

struct MemBackedKVStore {
    transport: Arc<ChannelTransport>,
}

impl KVStoreHandlers for MemBackedKVStore {
    fn _response_tx(&self) -> Sender<OwnedDatagram> {
        todo!()
    }

    fn ping(&self) -> _DynFut<LocalRpcResponse<()>> {
        todo!()
    }

    fn entries(&self, page: u64, page_size: u16) -> _DynFut<LocalRpcResponse<Vec<KV>>> {
        todo!()
    }

    fn keys(&self, page: u64, page_size: u16) -> _DynFut<LocalRpcResponse<Vec<String>>> {
        todo!()
    }

    fn values(&self, page: u64, page_size: u16) -> _DynFut<LocalRpcResponse<Vec<String>>> {
        todo!()
    }

    fn insert(&self, key: String, value: String) -> _DynFut<LocalRpcResponse<bool>> {
        todo!()
    }

    fn insert_many(&self, entries: Vec<KV>) -> _DynFut<LocalRpcResponse<Vec<String>>> {
        todo!()
    }

    fn get(&self, key: String) -> _DynFut<LocalRpcResponse<String>> {
        todo!()
    }

    fn count(&self) -> _DynFut<LocalRpcResponse<u64>> {
        todo!()
    }
}

struct NullServiceHandlers;
impl crate::generated::rpc::owned::NullServiceHandlers for NullServiceHandlers {
    fn _response_tx(&self) -> Sender<OwnedDatagram> {
        todo!()
    }
}

// TODO: this should be automatic and not require this impl
impl ServiceHandlers for MemBackedKVStore {
    const NAME: &'static str = <dyn KVStoreHandlers>::NAME;

    fn _recv_call(&self, datagram: &Datagram) -> Pin<Box<dyn Future<Output = ()>>> {
        (self as &dyn KVStoreHandlers)._recv_call(datagram)
    }
}

#[tokio::test]
async fn main() {
    let kv_store = Arc::new(MemBackedKVStore);
    let server = Router::<
        ChannelTransport,
        Arc<MemBackedKVStore>,
        NullServiceRequests<ChannelTransport, Arc<MemBackedKVStore>>,
    >::new(ChannelTransport, kv_store, None, |fut| tokio::spawn(fut));

    let client = Router::<
        ChannelTransport,
        NullServiceHandlers,
        KVStoreRequests<ChannelTransport, Arc<MemBackedKVStore>>,
    >::new(ChannelTransport, NullServiceHandlers, None, |fut| {
        tokio::spawn(fut)
    });

    client
        .insert("Mykey".into(), "Myvalue".into())
        .await
        .unwrap();
    assert_eq!(client.count().await.unwrap(), 1);
    assert_eq!(&client.get("Mykey".into()).await.unwrap(), "Myvalue");
}
