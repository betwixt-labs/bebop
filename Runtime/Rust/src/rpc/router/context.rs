use std::future::Future;
use std::num::NonZeroU16;
use std::pin::Pin;
use std::sync::Arc;
use std::time::Duration;

use parking_lot::Mutex;

use crate::rpc::datagram::Datagram;
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
        #[allow(unused)] spawn_task: impl Fn(Pin<Box<dyn 'static + Future<Output = ()>>>),
    ) -> Arc<Self> {
        let zelf = Arc::new(Self {
            unknown_response_handler,
            local_service,
            transport,
            call_table: Mutex::default(),
        });

        zelf.init_transport();
        #[cfg(feature = "rpc-timeouts")]
        spawn_task(zelf.init_cleanup());
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
    #[cfg(feature = "rpc-timeouts")]
    fn init_cleanup(self: &Arc<Self>) -> Pin<Box<dyn 'static + Future<Output = ()>>> {
        let mut interval = tokio::time::interval(Duration::from_secs(60));
        let ctx = Arc::downgrade(self);
        Box::pin(async move {
            loop {
                interval.tick().await;
                if let Some(ctx) = ctx.upgrade() {
                    ctx.call_table.lock().clean();
                } else {
                    break;
                }
            }
        })
    }

    /// Get the ID which should be used for the next call that gets made.
    fn next_call_id(&self) -> NonZeroU16 {
        self.call_table.lock().next_call_id()
    }

    /// Send a datagram to the remote. Can be either a request or response.
    /// This is used by the generated code.
    pub async fn send(&self, datagram: &mut D) {
        datagram.set_call_id(self.next_call_id());
        todo!()
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
