use std::future::Future;
use std::num::NonZeroU16;
use std::pin::Pin;
use std::sync::{Arc, Weak};
use std::time::Instant;

use parking_lot::Mutex;

use crate::rpc::datagram::RpcRequestHeader;
use crate::rpc::error::TransportResult;
use crate::rpc::router::call_table::RouterCallTable;
use crate::rpc::router::ServiceHandlers;
use crate::rpc::transport::TransportProtocol;
use crate::rpc::Datagram;
use crate::{OwnedRecord, Record, SliceWrapper};

pub struct RouterContext<Transport, Local> {
    /// Callback that receives any datagrams without a call id.
    unknown_response_handler: Option<Box<dyn Fn(&Datagram)>>,

    /// Local service handles requests from the remote.
    local_service: Local,

    transport: Transport,

    call_table: Mutex<RouterCallTable>,

    // /// Keep these so we can abort them on cleanup and prevent unclean exits.
    // /// TODO: Do we want this? Adds some overhead without any benefit besides quick shutdown.
    // expire_futures: Mutex<HashMap<NonZeroU16, tokio::task::JoinHandle<()>>>,
    /// Callback we should use to spawn futures for cleanup.
    spawn_task: Box<dyn Fn(Pin<Box<dyn 'static + Future<Output = ()>>>)>,
}

impl<T, L> RouterContext<T, L>
where
    T: 'static + TransportProtocol,
    L: 'static + ServiceHandlers,
{
    pub(super) fn new(
        transport: T,
        local_service: L,
        unknown_response_handler: Option<Box<dyn Fn(D)>>,
        spawn_task: impl 'static + Fn(Pin<Box<dyn 'static + Future<Output = ()>>>),
    ) -> Arc<Self> {
        let zelf = Arc::new(Self {
            unknown_response_handler,
            local_service,
            transport,
            call_table: Default::default(),
            // expire_futures: Default::default(),
            spawn_task: Box::new(spawn_task),
        });

        zelf.init_transport();
        zelf
    }

    /// One-time setup of the transport handler.
    fn init_transport(self: &Arc<Self>) {
        let weak_ctx = Arc::downgrade(self);
        self.transport.set_handler(Box::pin(move |datagram| {
            let weak_ctx = weak_ctx.clone();
            Box::pin(async move {
                if let Some(ctx) = weak_ctx.upgrade() {
                    ctx.recv(datagram).await;
                } else {
                    // No more router, just ignore
                }
            })
        }));
    }

    /// Send a request to the remote. This is used by the generated code.
    ///
    /// TODO: should we expose the PendingCall structure instead? Means this will be a future that
    ///  returns a future which might be a little unnecessary, but it will expose more info about
    ///  the underlying data.
    pub async fn request<T: OwnedRecord>(
        self: &Arc<Self>,
        opcode: u16,
        timeout: Option<NonZeroU16>,
        signature: u32,
        buf: &[u8],
    ) -> TransportResult<T> {
        let mut call_table = self.call_table.lock();
        let datagram = Datagram::RpcRequestDatagram {
            header: RpcRequestHeader {
                id: call_table.next_call_id() as u16,
                timeout: timeout.into(),
                signature,
            },
            opcode,
            request: SliceWrapper::Cooked(buf),
        };
        let pending = call_table.register(&datagram);
        drop(call_table);

        if let Some(at) = pending.expires_at() {
            (self.spawn_task)(Box::pin(Self::clean_on_expiration(
                Arc::downgrade(self),
                datagram.call_id().unwrap(),
                at,
            )));
        }
        self.transport.send(datagram).await?;
        pending.await
    }

    pub(in super::calls) async fn send(&self, datagram: &Datagram) -> TransportResult {
        self.transport.send(datagram).await
    }

    // pub async fn respond(&self, datagram: &D) -> TransportResult {
    //     debug_assert!(
    //         datagram.is_response(),
    //         "This function requires a response datagram."
    //     );
    //     self.transport.send(datagram).await
    // }

    /// Receive a datagram and handles it appropriately. Async to apply backpressure on requests.
    /// This is used by the handler for the TransportProtocol.
    async fn recv(&self, datagram: &Datagram) {
        if datagram.is_request() {
            self.local_service._recv_call(datagram).await;
        } else {
            self.call_table
                .lock()
                .resolve(&self.unknown_response_handler, datagram);
        }
    }

    /// Notify the call table of an expiration event and have it remove the datagram if appropriate.
    async fn clean_on_expiration(zelf: Weak<Self>, id: NonZeroU16, at: Instant) {
        tokio::time::sleep_until(at.into()).await;
        if let Some(zelf) = zelf.upgrade() {
            // zelf.expire_futures.lock().remove(&id).unwrap();
            zelf.call_table.lock().drop_expired(id);
        }
    }

    /// Send notification that there was an error decoding one of the datagrams. This may be called
    /// by the transport or by the generated handler code.
    pub async fn send_decode_error_response(
        &self,
        call_id: Option<NonZeroU16>,
        info: Option<&str>,
    ) -> TransportResult {
        todo!()
        // self.transport.send_decode_error_response(...).await
    }
}

// impl<D, T, L> Drop for RouterContext<D, T, L> {
//     fn drop(&mut self) {
//         for handle in self.expire_futures.lock().values() {
//             handle.abort()
//         }
//     }
// }
