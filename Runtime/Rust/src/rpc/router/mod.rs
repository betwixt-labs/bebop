use std::future::Future;
use std::ops::Deref;
use std::pin::Pin;
use std::sync::{Arc, Weak};

use async_trait::async_trait;

pub use context::RouterContext;

use crate::rpc::transport::TransportProtocol;

mod call_table;
mod context;
mod pending_response;

/// The local end of the pipe handles messages. Implementations are automatically generated from
/// bebop service definitions.
///
/// You should not implement this by hand.
///
/// TODO: can this use Datagram<'raw> instead of OwnedDatagram.
#[async_trait]
pub trait ServiceHandlers {
    const NAME: &'static str;

    /// Use opcode to determine which function to call, whether the signature matches,
    /// how to read the buffer, and then convert the returned values and send them as a
    /// response
    ///
    /// This should only be called by the `Router` and is not for external use.
    async fn _recv_call(&self, datagram: D);
}

/// Wrappers around the process of calling remote functions. Implementations are generated from
/// bebop service definitions.
///
/// You should not implement this by hand.
pub trait ServiceRequests<T, L>
where
    T: 'static + TransportProtocol,
    L: 'static + ServiceHandlers,
{
    const NAME: &'static str;

    fn new(ctx: Weak<RouterContext<T, L>>) -> Self;
}

/// This is the main structure which represents information about both ends of the connection and
/// maintains the needed state to make and receive calls. This is the only struct of which an
/// instance should need to be maintained by the user.
pub struct Router<Transport, Local, Remote> {
    /// Remote service converts requests from us, so this also provides the callable RPC functions.
    _remote_service: Remote,

    /// Router state that can be referenced by multiple places. This should only be used by
    /// generated code.
    ///
    /// **Warning:** always store weak references or the router will have issues shutting down due
    /// to cyclical dependencies.
    pub _context: Arc<RouterContext<Transport, Local>>,
}

impl<T, L, R> Router<T, L, R>
where
    T: 'static + TransportProtocol,
    L: 'static + ServiceHandlers,
    R: ServiceRequests<T, L>,
{
    /// Create a new router instance.
    ///
    /// - `transport` The underlying transport this router uses.
    /// - `local_service` The service which handles incoming requests.
    /// - `remote_service` The service the remote server provides which we can call.
    /// - `unknown_response_handler` Optional callback to handle error cases where we do not know
    /// what the `call_id` is or it is an invalid `call_id`.
    /// - `spawn_task` Run a task in the background. It will know when to stop on its own. This may
    /// not always be called depending on configuration and features.
    pub fn new(
        transport: T,
        local_service: L,
        unknown_response_handler: Option<Box<dyn Fn(D)>>,
        spawn_task: impl 'static + Fn(Pin<Box<dyn 'static + Future<Output = ()>>>),
    ) -> Self {
        let ctx = RouterContext::new(
            transport,
            local_service,
            unknown_response_handler,
            spawn_task,
        );
        Self {
            _remote_service: R::new(Arc::downgrade(&ctx)),
            _context: ctx,
        }
    }
}

/// Allows passthrough of function calls to the remote
impl<T, L, R> Deref for Router<T, L, R> {
    type Target = R;

    fn deref(&self) -> &Self::Target {
        &self._remote_service
    }
}
