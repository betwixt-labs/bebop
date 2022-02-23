use std::ops::Deref;
use std::sync::{Arc, Weak};

pub use context::RouterContext;

use crate::rpc::router::context::{RouterTransport, SpawnTask, UnknownResponseHandler};
use crate::rpc::transport::TransportProtocol;
use crate::rpc::{Datagram, DynFuture};

mod call_table;
pub mod calls;
mod context;

/// The local end of the pipe handles messages. Implementations are automatically generated from
/// bebop service definitions.
///
/// You should not implement this by hand but rather the *Handlers traits which are generated.
pub trait ServiceHandlers: Send + Sync {
    const NAME: &'static str;

    /// Use opcode to determine which function to call, whether the signature matches,
    /// how to read the buffer, and then convert the returned values and send them as a
    /// response
    ///
    /// This should only be called by the `Router` and is not for external use.
    ///
    /// This returns a future instead of being async because it must decode datagram before starting
    /// the async section.
    fn _recv_call<'a>(
        &self,
        datagram: &Datagram,
        transport: Weak<dyn RouterTransport>,
    ) -> DynFuture<'a>;
}

impl<T: Deref<Target = S> + Send + Sync, S: ServiceHandlers> ServiceHandlers for T {
    const NAME: &'static str = S::NAME;

    fn _recv_call<'a>(
        &self,
        datagram: &Datagram,
        transport: Weak<dyn RouterTransport>,
    ) -> DynFuture<'a> {
        self.deref()._recv_call(datagram, transport)
    }
}

/// Wrappers around the process of calling remote functions. Implementations are generated from
/// bebop service definitions.
///
/// You should not implement this by hand.
pub trait ServiceRequests {
    const NAME: &'static str;

    fn new(ctx: Weak<dyn RouterTransport>) -> Self;
}

impl<D, S> ServiceRequests for D
where
    D: Deref<Target = S>,
    S: ServiceRequests + Into<D>,
{
    const NAME: &'static str = S::NAME;

    fn new(ctx: Weak<dyn RouterTransport>) -> Self {
        S::new(ctx).into()
    }
}

/// This is the main structure which represents information about both ends of the connection and
/// maintains the needed state to make and receive calls. This is the only struct of which an
/// instance should need to be maintained by the user.
pub struct Router<Transport: Send + Sync, Local: Send + Sync, Remote> {
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
    R: 'static + ServiceRequests,
{
    /// Create a new router instance.
    ///
    /// - `transport` The underlying transport this router uses.
    /// - `local_service` The service which handles incoming requests.
    /// - `remote_service` The service the remote server provides which we can call.
    /// - `spawn_task` Run a task in the background. It will know when to stop on its own. This may
    /// not always be called depending on configuration and features.
    /// - `unknown_response_handler` Optional callback to handle error cases where we do not know
    /// what the `call_id` is or it is an invalid `call_id`.
    pub fn new(
        transport: T,
        local_service: L,
        spawn_task: SpawnTask,
        unknown_response_handler: Option<UnknownResponseHandler>,
    ) -> Self {
        let ctx = RouterContext::new(
            transport,
            local_service,
            spawn_task,
            unknown_response_handler,
        );
        Self {
            _remote_service: R::new(Arc::downgrade(&ctx) as Weak<dyn RouterTransport>),
            _context: ctx,
        }
    }
}

/// Allows passthrough of function calls to the remote
impl<T: Send + Sync, L: Send + Sync, R> Deref for Router<T, L, R> {
    type Target = R;

    fn deref(&self) -> &Self::Target {
        &self._remote_service
    }
}
