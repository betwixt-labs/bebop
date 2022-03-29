use std::ops::Deref;
use std::sync::{Arc, Weak};

use static_assertions::assert_obj_safe;

pub use context::RouterContext;
pub use context::TransportHandler;

use crate::rpc::transport::TransportProtocol;
use crate::rpc::{Datagram, DynFuture};

pub use self::calls::{CallDetails, RequestHandle, TypedRequestHandle};
use self::context::{SpawnTask, UnknownResponseHandler};

mod call_table;
pub mod calls;
mod context;

/// The local end of the pipe handles messages. Implementations are automatically generated from
/// bebop service definitions.
///
/// You should not implement this by hand but rather the *Handlers traits which are generated.
pub trait ServiceHandlers: Send + Sync {
    /// The name of this service.
    ///
    /// This is a const for a given implementation, however for dynamic type support it has to be
    /// provided via a function.
    fn _name(&self) -> &'static str;

    /// Use opcode to determine which function to call, whether the signature matches,
    /// how to read the buffer, and then convert the returned values and send them as a
    /// response
    ///
    /// This should only be called by the `Router` and is not for external use.
    ///
    /// This returns a future instead of being async because it must decode datagram before starting
    /// the async section.
    #[doc(hidden)]
    fn _recv_call<'f>(&self, datagram: &Datagram, handle: RequestHandle) -> DynFuture<'f>;
}

// impl<T: Deref<Target = S> + Send + Sync, S: ServiceHandlers> ServiceHandlers for T {
//     fn _name(&self) -> &'static str {
//         self.deref()._name()
//     }
//
//     fn _recv_call<'f>(&self, datagram: &Datagram, transport: RequestHandle) -> DynFuture<'f> {
//         self.deref()._recv_call(datagram, transport)
//     }
// }

assert_obj_safe!(ServiceHandlers);

/// Wrappers around the process of calling remote functions. Implementations are generated from
/// bebop service definitions.
///
/// You should not implement this by hand.
pub trait ServiceRequests: Send + Sync {
    /// The name of this service.
    const NAME: &'static str;

    fn new(ctx: Weak<RouterContext>) -> Self;
}

impl<D, S> ServiceRequests for D
where
    D: Deref<Target = S> + Send + Sync,
    S: ServiceRequests + Into<D>,
{
    const NAME: &'static str = S::NAME;

    fn new(ctx: Weak<RouterContext>) -> Self {
        S::new(ctx).into()
    }
}

/// This is the main structure which represents information about both ends of the connection and
/// maintains the needed state to make and receive calls. This is the only struct of which an
/// instance should need to be maintained by the user.
pub struct Router<Remote> {
    /// Remote service converts requests from us, so this also provides the callable RPC functions.
    _remote_service: Remote,

    /// Router state that can be referenced by multiple places. This should only be used by
    /// generated code.
    ///
    /// **Warning:** always store weak references or the router will have issues shutting down due
    /// to cyclical dependencies.
    #[doc(hidden)]
    pub _context: Arc<RouterContext>,
}

impl<R> Router<R>
where
    R: 'static + ServiceRequests,
{
    /// Create a new router instance.
    ///
    /// - `transport` The underlying transport this router uses.
    /// - `local_service` The service which handles incoming requests.
    /// - `spawn_task` Run a task in the background. It will know when to stop on its own. This may
    /// not always be called depending on configuration and features.
    /// - `unknown_response_handler` Optional callback to handle error cases where we do not know
    /// what the `call_id` is or when it is an invalid `call_id`.
    pub fn new(
        transport: impl 'static + TransportProtocol,
        local_service: impl 'static + ServiceHandlers,
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
            _remote_service: R::new(Arc::downgrade(&ctx)),
            _context: ctx,
        }
    }
}

impl<R: Clone> Clone for Router<R> {
    fn clone(&self) -> Self {
        Self {
            _remote_service: self._remote_service.clone(),
            _context: self._context.clone(),
        }
    }
}

/// Allows passthrough of function calls to the remote
impl<R> Deref for Router<R> {
    type Target = R;

    fn deref(&self) -> &Self::Target {
        &self._remote_service
    }
}
