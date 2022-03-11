use std::collections::hash_map::Entry;
use std::collections::HashMap;
use std::sync::Arc;
use std::time::{Duration, Instant};

use bebop::prelude::*;
use bebop::rpc::{handlers, RemoteRpcError, Router};
use bebop::rpc::{
    CallDetails, LocalRpcError, TransportError, TransportHandler, TransportProtocol,
    TransportResult, TypedRequestHandle,
};
use bebop::timeout;
// Usually I would use parking lot, but since we are simulating a database I think this will give
// a better impression of what the operations will look like with the required awaits.
use tokio::sync::RwLock;

use crate::generated::rpc::owned::KVStoreHandlersDef;
pub use crate::generated::rpc::owned::NullServiceRequests;
use crate::generated::rpc::owned::{KVStoreHandlers, KVStoreRequests, NullServiceHandlers};
use crate::generated::rpc::KV;

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
#[handlers(crate::generated::rpc)]
impl KVStoreHandlersDef for Arc<MemBackedKVStore> {
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
        details: &dyn CallDetails,
        page: u64,
        page_size: u16,
    ) -> LocalRpcResponse<Vec<&'sup str>> {
        let lock = self.0.read().await;
        // For now we do not support `return` statements, so you will have to have an expression
        // which resolves correctly. An easy fix for more complicated logic is to call another
        // function, but do be aware that the value of this expression will live until the
        // response is sent.

        if details.deadline().has_passed() {
            Err(LocalRpcError::DeadlineExceded)
        } else {
            // Note that if this operation takes a long time, it might still result in a
            // DeadlineExceeded error being returned in the end.
            Ok(lock
                .keys()
                .skip(page as usize * page_size as usize)
                .take(page_size as usize)
                .map(|k| k.as_str())
                .collect())
        }
    }

    #[handler]
    async fn values<'sup>(
        self,
        _details: &dyn CallDetails,
        _page: u64,
        _page_size: u16,
    ) -> LocalRpcResponse<Vec<&'sup str>> {
        // this will be given a "not implemented" implementation which is generally preferable to
        // triggering a panic in the server.
        // Equiv to writing `Err(LocalRpcError::NotSupported)`
    }

    #[handler]
    async fn insert(
        self,
        _details: &dyn CallDetails,
        key: String,
        value: String,
    ) -> LocalRpcResponse<bool> {
        // don't need the lock to hang around because we are returning a type without a lifetime
        // dependent on the lock
        Ok(match self.0.write().await.entry(key) {
            Entry::Occupied(_) => false,
            Entry::Vacant(entry) => {
                entry.insert(value);
                true
            }
        })
    }

    #[handler]
    async fn insert_many<'sup>(
        self,
        _details: &dyn CallDetails,
        entries: Vec<crate::generated::rpc::owned::KV>,
    ) -> LocalRpcResponse<Vec<&'sup str>> {
        let mut lock = self.0.write().await;
        let preexisting: Vec<&'sup str> = entries
            .into_iter()
            .filter_map(
                |crate::generated::rpc::owned::KV { key, value }| match lock.entry(key) {
                    Entry::Occupied(entry) => Some(unsafe {
                        // Okay, so this looks bad, but hear me out, we know it will live this long
                        // because we hold a lock for the entire duration; for a purely safe option
                        // we would have to clone the data which is stupid and pointless with the
                        // guarantees we have here.
                        &*(entry.key().as_str() as *const str)
                    }),
                    Entry::Vacant(entry) => {
                        entry.insert(value);
                        None
                    }
                },
            )
            .collect();

        // we can allow people to read now that we are done mutating. DO NOT allow it to drop though
        // until the references go out of scope which happens after our "return".
        let _lock = lock.downgrade();
        Ok(preexisting)
    }

    fn get<'f>(
        &self,
        handle: TypedRequestHandle<'f, crate::generated::rpc::KVStoreGetReturn<'f>>,
        key: String,
    ) -> DynFuture<'f, ()> {
        // if you really really want to, you can do it by hand as well.
        let zelf = self.clone();
        Box::pin(async move {
            let lock = zelf.0.read().await;
            let response = lock
                .get(&key)
                .ok_or(LocalRpcError::CustomErrorStatic(1, "Unknown key"))
                .map(|value| crate::generated::rpc::KVStoreGetReturn { value });
            let call_id = handle.call_id().get();

            // this way does allow you to write a custom error handler instead of using the default
            // static one.
            bebop::handle_respond_error!(
                handle.send_response(response.as_ref()),
                "KVStore",
                "get",
                call_id
            );
        })
    }

    #[handler]
    async fn count(self, _details: &dyn CallDetails) -> LocalRpcResponse<u64> {
        Ok(self.0.read().await.len() as u64)
    }

    #[handler]
    async fn wait(self, details: &dyn CallDetails, secs: u16) -> LocalRpcResponse<()> {
        let will_timeout = if let Some(instant) = details.deadline().as_ref() {
            // normally would just return here if it will timeout, but we need to avoid return
            // statements, so...
            *instant - Instant::now() >= Duration::from_secs(secs as u64)
        } else {
            false
        };

        if will_timeout {
            // we will eventually time out, so just do it now
            Err(LocalRpcError::DeadlineExceded)
        } else {
            let _lock = self.0.write().await;
            tokio::time::sleep(Duration::from_secs(secs as u64)).await;
            Ok(())
        }
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
async fn defaults_to_not_supported() {
    let (_server, client) = setup();
    assert!(matches!(
        client.values(timeout!(None), 1, 0).await,
        Err(RemoteRpcError::CallNotSupported)
    ));
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
