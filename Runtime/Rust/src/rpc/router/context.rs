use std::num::NonZeroU16;
use std::pin::Pin;
use std::sync::{Arc, Weak};
use std::time::{Duration, Instant};

use parking_lot::Mutex;

use crate::prelude::DynFuture;
use crate::rpc::calls::CallDetails;
use crate::rpc::datagram::{RpcRequestHeader, RpcResponseHeader};
use crate::rpc::datagram_info::DatagramInfo;
use crate::rpc::error::{RemoteRpcResponse, TransportResult};
use crate::rpc::router::call_table::RouterCallTable;
use crate::rpc::router::ServiceHandlers;
use crate::rpc::transport::TransportProtocol;
use crate::rpc::{convert_timeout, Datagram, RequestHandle};
use crate::{OwnedRecord, Record, SliceWrapper};

pub type UnknownResponseHandler = Pin<Box<dyn Send + Sync + Fn(&Datagram)>>;
pub type SpawnTask = Pin<Box<dyn Send + Sync + Fn(DynFuture<'static>)>>;

pub struct RouterContext {
    /// Callback that receives any datagrams without a call id.
    unknown_response_handler: Option<UnknownResponseHandler>,

    /// Local service handles requests from the remote.
    local_service: Box<dyn ServiceHandlers>,

    transport: Box<dyn TransportProtocol>,

    call_table: Mutex<RouterCallTable>,

    // /// Keep these so we can abort them on cleanup and prevent unclean exits.
    // /// TODO: Do we want this? Adds some overhead without any benefit besides quick shutdown.
    // expire_futures: Mutex<HashMap<NonZeroU16, tokio::task::JoinHandle<()>>>,
    /// Callback we should use to spawn futures for cleanup.
    spawn_task: SpawnTask,
}

/// A connector between the Router and the Handlers. This is set up internally.
///
/// TODO: Replace with Fn traits when available
#[derive(Clone)]
pub struct TransportHandler(Weak<RouterContext>);
impl TransportHandler {
    pub fn handle<'a, 'b: 'a>(&self, datagram: &'a Datagram<'b>) -> Option<DynFuture<'a>> {
        if let Some(ctx) = self.0.upgrade() {
            if datagram.is_request() {
                let handle = RequestHandle::new(self.0.clone(), datagram);
                Some(ctx.recv_request(datagram, handle))
            } else {
                ctx.recv_response(datagram);
                None
            }
        } else {
            // No more router, just ignore
            None
        }
    }
}

impl RouterContext {
    pub(super) fn new(
        transport: impl 'static + TransportProtocol,
        local_service: impl 'static + ServiceHandlers,
        spawn_task: SpawnTask,
        unknown_response_handler: Option<UnknownResponseHandler>,
    ) -> Arc<Self> {
        let zelf = Arc::new(Self {
            transport: Box::new(transport),
            local_service: Box::new(local_service),
            call_table: Default::default(),
            // expire_futures: Default::default(),
            spawn_task,
            unknown_response_handler,
        });

        // we need a reference to self, this is safe because even though there is a mutable ref and
        // a const ref at the same time, we control both of them and ensure the mutable ref ends
        // before the const ref (or any other ref) can be used.
        unsafe {
            let zelf_ptr = Arc::as_ptr(&zelf) as *mut Self;
            (*zelf_ptr)
                .transport
                .set_handler(TransportHandler(Arc::downgrade(&zelf)));
        }

        zelf
    }

    /// Send a request to the remote. This is used by the generated code.
    #[doc(hidden)]
    pub async fn request<'a, 'b: 'a, I, O>(
        self: Arc<Self>,
        opcode: u16,
        timeout: Option<Duration>,
        signature: u32,
        record: &'a I,
    ) -> RemoteRpcResponse<O>
    where
        I: Record<'b>,
        O: 'static + OwnedRecord,
    {
        self.request_raw(
            opcode,
            convert_timeout(timeout),
            signature,
            &record.serialize_to_vec()?,
        )
        .await
    }

    /// Send a raw byte request to the remote. This is used by the generated code.
    #[doc(hidden)]
    pub async fn request_raw<R>(
        self: Arc<Self>,
        opcode: u16,
        timeout: Option<NonZeroU16>,
        signature: u32,
        data: &[u8],
    ) -> RemoteRpcResponse<R>
    where
        R: 'static + OwnedRecord,
    {
        let mut call_table = self.call_table.lock();
        let datagram = Datagram::RpcRequestDatagram {
            header: RpcRequestHeader {
                id: u16::from(call_table.next_call_id()),
                timeout: timeout.map(u16::from).unwrap_or(0),
                signature,
            },
            opcode,
            data: SliceWrapper::Cooked(data),
        };
        let pending = call_table.register(&datagram);
        drop(call_table);

        if let Some(at) = pending.expires_at() {
            (self.spawn_task)(Box::pin(Self::clean_on_expiration(
                Arc::downgrade(&self),
                datagram.call_id().unwrap(),
                at,
            )));
        }

        self.send(&datagram).await?;
        pending.await
    }

    pub(super) async fn send<'a, 'b: 'a>(&self, datagram: &'a Datagram<'b>) -> TransportResult {
        self.transport.send(datagram).await
    }

    /// Send notification that there was an error decoding one of the datagrams. This may be called
    /// by the transport or by the generated handler code.
    pub async fn send_decode_error_response(
        &self,
        call_id: Option<NonZeroU16>,
        info: Option<&str>,
    ) -> TransportResult {
        self.send(&Datagram::RpcDecodeError {
            header: RpcResponseHeader {
                id: call_id.map(Into::into).unwrap_or(0),
            },
            info: info.unwrap_or(""),
        })
            .await
    }

    /// Receive a request datagram and send it to the local service for handling.
    /// This is used by the handler for the TransportProtocol.
    fn recv_request<'a, 'b: 'a>(
        &self,
        datagram: &'a Datagram<'b>,
        handle: RequestHandle,
    ) -> DynFuture<'b> {
        debug_assert!(datagram.is_request(), "Datagram must be a request");
        self.local_service._recv_call(datagram, handle)
    }

    /// Receive a response datagram and pass it to the call table to resolve the correct future.
    /// This is used by the handler for the TransportProtocol.
    fn recv_response<'a, 'b: 'a>(&self, datagram: &'a Datagram<'b>) {
        debug_assert!(datagram.is_response(), "Datagram must be a response");
        self.call_table
            .lock()
            .resolve(&self.unknown_response_handler, datagram);
    }

    /// Notify the call table of an expiration event and have it remove the datagram if appropriate.
    async fn clean_on_expiration(zelf: Weak<Self>, id: NonZeroU16, at: Instant) {
        tokio::time::sleep_until(at.into()).await;
        if let Some(zelf) = zelf.upgrade() {
            // zelf.expire_futures.lock().remove(&id).unwrap();
            zelf.call_table.lock().drop_expired(id);
        }
    }
}

// impl<D, T, L> Drop for RouterContext<D, T, L> {
//     fn drop(&mut self) {
//         for handle in self.expire_futures.lock().values() {
//             handle.abort()
//         }
//     }
// }
