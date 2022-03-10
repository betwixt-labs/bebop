use std::collections::hash_map::Entry;
use std::collections::HashMap;
use std::sync::Arc;
use std::time::{Duration, Instant};

use bebop::prelude::*;
use bebop::rpc::LocalRpcError::DeadlineExceded;
use bebop::rpc::{handlers, RemoteRpcError, Router};
use bebop::rpc::{
    CallDetails, Deadline, LocalRpcError, LocalRpcResponse, TransportError, TransportHandler,
    TransportProtocol, TransportResult, TypedRequestHandle,
};
use bebop::timeout;
use parking_lot::RwLockReadGuard;
// Usually I would use parking lot, but since we are simulating a database I think this will give
// a better impression of what the operations will look like with the required awaits.
use tokio::sync::RwLock;

use crate::generated::rpc as rtype;
use crate::generated::rpc::owned::KVStoreHandlersDef;
pub use crate::generated::rpc::owned::NullServiceRequests;
use crate::generated::rpc::owned::{KVStoreHandlers, KVStoreRequests, NullServiceHandlers};
use crate::generated::rpc::KV;
use crate::generated::rpc::{
    KVStoreCountReturn, KVStoreEntriesReturn, KVStoreGetReturn, KVStoreInsertManyReturn,
    KVStoreInsertReturn, KVStoreKeysReturn, KVStoreValuesReturn, KVStoreWaitReturn,
};

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

// TODO: allow passing the path to the return types so they do not have to be in scope.
use crate::generated;

// impl for Arc so that we can create weak references that we pass to the futures without capturing
// the lifetime of the self reference.
#[handlers(crate::generated::rpc)]
impl KVStoreHandlersDef for Arc<MemBackedKVStore> {
    // fn ping<'f>(&self, _handle: ::bebop::rpc::TypedRequestHandle<'f, super::KVStorePingReturn>) -> ::bebop::rpc::DynFuture<'f, ()>;

    #[handler]
    async fn ping(self, _details: &dyn CallDetails) -> LocalRpcResponse<()> {
        Err(LocalRpcError::CustomErrorStatic(4, "some error"))
    }

    #[handler]
    async fn entries<'sup>(
        self,
        _details: &dyn CallDetails,
        page: u64,
        page_size: u16,
    ) -> LocalRpcResponse<Vec<KV<'sup>>> {
        // I know it looks like this lock could be done inline, but we need it here because it must
        // outlive the returned value. If you don't do this you will get a lifetime error which
        // might seem cryptic but is easily fixed. (Sorry, line numbers will be wrong).
        //
        // Example Error:
        // error[E0716]: temporary value dropped while borrowed
        //    --> functionality-testing\src\rpc.rs:116:1
        //     |
        // 116 |   #[handlers(crate::generated::rpc)]
        //     |   ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^-
        //     |   |                                |
        //     |   |                                temporary value is freed at the end of this statement
        //     |   creates a temporary which is freed while still in use
        //     |   in this procedural macro expansion
        // ...
        // 126 |       async fn entries<'sup>(self, _details: &dyn CallDetails, page: u64, page_size: u16) -> LocalRpcResponse<Vec<KV<'sup>>> {
        //     |  ____________________________________________________________________________________________________________________________-
        // ...   |
        // 140 | |         )
        // 141 | |     }
        //     | |_____- borrow later used here
        //     |
        //    ::: C:\Users\Matthew\workspace\bebop\Runtime\Rust\handler-macro\src\lib.rs:54:1
        //     |
        // 54  | / pub fn handlers(
        // 55  | |     args: proc_macro::TokenStream,
        // 56  | |     input: proc_macro::TokenStream,
        // 57  | | ) -> proc_macro::TokenStream {
        //     | |____________________________- in this expansion of `#[handlers]`
        //     |
        //     = note: consider using a `let` binding to create a longer lived value

        let lock = self.0.read().await;
        Ok(lock
            .iter()
            .skip(page as usize * page_size as usize)
            .take(page_size as usize)
            .map(|(k, v)| KV { key: k, value: v })
            .collect())
    }

    #[handler]
    async fn keys<'sup>(
        self,
        _details: &dyn CallDetails,
        page: u64,
        page_size: u16,
    ) -> LocalRpcResponse<::std::vec::Vec<&'sup str>> {
        let lock = self.0.read().await;
        Ok(lock
            .keys()
            .skip(page as usize * page_size as usize)
            .take(page_size as usize)
            .map(|k| k.as_str())
            .collect())
    }

    fn values<'f>(
        &self,
        handle: TypedRequestHandle<'f, KVStoreValuesReturn<'f>>,
        page: u64,
        page_size: u16,
    ) -> DynFuture<'f, ()> {
        todo!()
    }

    fn insert<'f>(
        &self,
        handle: TypedRequestHandle<'f, KVStoreInsertReturn>,
        key: String,
        value: String,
    ) -> DynFuture<'f, ()> {
        todo!()
    }

    fn insert_many<'f>(
        &self,
        handle: TypedRequestHandle<'f, KVStoreInsertManyReturn<'f>>,
        entries: Vec<crate::generated::rpc::owned::KV>,
    ) -> DynFuture<'f, ()> {
        todo!()
    }

    fn get<'f>(
        &self,
        handle: TypedRequestHandle<'f, KVStoreGetReturn<'f>>,
        key: String,
    ) -> DynFuture<'f, ()> {
        todo!()
    }

    fn count<'f>(&self, handle: TypedRequestHandle<'f, KVStoreCountReturn>) -> DynFuture<'f, ()> {
        todo!()
    }

    fn wait<'f>(
        &self,
        handle: TypedRequestHandle<'f, KVStoreWaitReturn>,
        secs: u16,
    ) -> DynFuture<'f, ()> {
        todo!()
    }

    // fn entries<'f>(
    //     &self,
    //     _deadline: Deadline,
    //     page: u64,
    //     page_size: u16,
    // ) -> DynFuture<'f, LocalRpcResponse<Vec<KV>>> {
    //     // NOTE: it is not valid to capture `self` in the future! This is enforced by lifetime
    //     // constraints.
    //
    //     let zelf = self.clone();
    //     Box::pin(async move {
    //         Ok(zelf
    //             .0
    //             .read()
    //             .await
    //             .iter()
    //             .skip(page as usize * page_size as usize)
    //             .take(page_size as usize)
    //             .map(|(k, v)| KV {
    //                 key: k.clone(),
    //                 value: v.clone(),
    //             })
    //             .collect())
    //     })
    // }
    //
    // fn keys<'f>(
    //     &self,
    //     _deadline: Deadline,
    //     page: u64,
    //     page_size: u16,
    // ) -> DynFuture<'f, LocalRpcResponse<Vec<String>>> {
    //     let zelf = self.clone();
    //     Box::pin(async move {
    //         Ok(zelf
    //             .0
    //             .read()
    //             .await
    //             .keys()
    //             .skip(page as usize * page_size as usize)
    //             .take(page_size as usize)
    //             .cloned()
    //             .collect())
    //     })
    // }
    //
    // fn values<'f>(
    //     &self,
    //     _deadline: Deadline,
    //     page: u64,
    //     page_size: u16,
    // ) -> DynFuture<'f, LocalRpcResponse<Vec<String>>> {
    //     let zelf = self.clone();
    //     Box::pin(async move {
    //         Ok(zelf
    //             .0
    //             .read()
    //             .await
    //             .values()
    //             .skip(page as usize * page_size as usize)
    //             .take(page_size as usize)
    //             .cloned()
    //             .collect())
    //     })
    // }
    //
    // fn insert<'f>(
    //     &self,
    //     _deadline: Deadline,
    //     key: String,
    //     value: String,
    // ) -> DynFuture<'f, LocalRpcResponse<bool>> {
    //     let zelf = self.clone();
    //     Box::pin(async move {
    //         Ok(match zelf.0.write().await.entry(key) {
    //             Entry::Occupied(_) => false,
    //             Entry::Vacant(entry) => {
    //                 entry.insert(value);
    //                 true
    //             }
    //         })
    //     })
    // }
    //
    // fn insert_many<'f>(
    //     &self,
    //     _deadline: Deadline,
    //     entries: Vec<KV>,
    // ) -> DynFuture<'f, LocalRpcResponse<Vec<String>>> {
    //     let zelf = self.clone();
    //     Box::pin(async move {
    //         let mut lock = zelf.0.write().await;
    //         let preexisting = entries
    //             .into_iter()
    //             .filter_map(|KV { key, value }| match lock.entry(key) {
    //                 Entry::Occupied(entry) => Some(entry.key().clone()),
    //                 Entry::Vacant(entry) => {
    //                     entry.insert(value);
    //                     None
    //                 }
    //             })
    //             .collect();
    //         Ok(preexisting)
    //     })
    // }
    //
    // fn get<'f>(&self, _deadline: Deadline, key: String) -> DynFuture<'f, LocalRpcResponse<String>> {
    //     let zelf = self.clone();
    //     Box::pin(async move {
    //         zelf.0
    //             .read()
    //             .await
    //             .get(&key)
    //             .cloned()
    //             .ok_or(LocalRpcError::CustomErrorStatic(1, "Unknown key"))
    //     })
    // }
    //
    // fn count<'f>(&self, _deadline: Deadline) -> DynFuture<'f, LocalRpcResponse<u64>> {
    //     let zelf = self.clone();
    //     Box::pin(async move { Ok(zelf.0.read().await.len() as u64) })
    // }
    //
    // fn wait<'f>(&self, deadline: Deadline, secs: u16) -> DynFuture<'f, LocalRpcResponse<()>> {
    //     let zelf = self.clone();
    //     Box::pin(async move {
    //         if let Some(deadline) = deadline.as_ref() {
    //             if *deadline - Instant::now() >= Duration::from_secs(secs as u64) {
    //                 // we will eventually time out, so just do it now
    //                 return Err(DeadlineExceded);
    //             }
    //         }
    //         let _lock = zelf.0.write().await;
    //         tokio::time::sleep(Duration::from_secs(secs as u64)).await;
    //         if deadline.has_passed() {
    //             Err(DeadlineExceded)
    //         } else {
    //             Ok(())
    //         }
    //     })
    // }
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
    let (server, client) = setup();
    assert_eq!(client.service_name(timeout!(0)).await.unwrap(), "KVStore");
    assert_eq!(
        server.service_name(timeout!(0)).await.unwrap(),
        "NullService"
    );
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
