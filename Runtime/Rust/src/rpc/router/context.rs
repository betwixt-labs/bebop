use std::future::Future;
use std::pin::Pin;
use std::sync::{Arc, Weak};
use std::time::Duration;

use parking_lot::Mutex;

use crate::rpc::datagram::Datagram;
use crate::rpc::error::TransportResult;
use crate::rpc::router::call_table::RouterCallTable;
use crate::rpc::router::ServiceHandlers;
use crate::rpc::transport::TransportProtocol;

pub struct RouterContext<Datagram, Transport, Local> {
    /// Callback that receives any datagrams without a call id.
    unknown_response_handler: Option<Box<dyn Fn(Datagram)>>,

    /// Local service handles requests from the remote.
    local_service: Local,

    transport: Transport,

    call_table: Mutex<RouterCallTable<Datagram>>,
}

impl<D, T, L> RouterContext<D, T, L>
where
    D: 'static + Datagram,
    T: 'static + TransportProtocol<D>,
    L: 'static + ServiceHandlers<D>,
{
    pub(super) fn new(
        transport: T,
        local_service: L,
        unknown_response_handler: Option<Box<dyn Fn(D)>>,
        spawn_task: impl Fn(Pin<Box<dyn 'static + Future<Output = ()>>>),
    ) -> Arc<Self> {
        let zelf = Arc::new(Self {
            unknown_response_handler,
            local_service,
            transport,
            call_table: Mutex::default(),
        });

        zelf.init_transport();
        spawn_task(Box::pin(Self::init_cleanup(Arc::downgrade(&zelf))));
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

    /// Create a task that will clean up any old requests every so often.
    async fn init_cleanup(weak_ctx: Weak<Self>) {
        let mut interval = tokio::time::interval(Duration::from_secs(60));
        loop {
            interval.tick().await;
            if let Some(ctx) = weak_ctx.upgrade() {
                ctx.call_table.lock().clean();
            } else {
                break;
            }
        }
    }

    /// Send a datagram to the remote. Can be either a request or response.
    /// This is used by the generated code.
    ///
    /// TODO: should we expose the PendingCall structure instead? Means this will be a future that
    ///  returns a future which might be a little unnecessary, but it will expose more info about
    ///  the underlying data.
    pub async fn request(&self, datagram: &mut D) -> TransportResult<D> {
        debug_assert!(
            datagram.is_request(),
            "This function requires a request datagram."
        );
        let pending = self.call_table.lock().register(datagram);
        self.transport.send(datagram).await;
        pending.await
    }

    pub async fn respond(&self, datagram: &D) {
        debug_assert!(
            datagram.is_response(),
            "This function requires a response datagram."
        );
        self.transport.send(datagram).await
    }

    /// Receive a datagram and routes it. This is used by the handler for the TransportProtocol.
    async fn recv(&self, datagram: D) {
        self.call_table
            .lock()
            .recv(
                &self.local_service,
                &self.unknown_response_handler,
                datagram,
            )
            .await
    }
}
