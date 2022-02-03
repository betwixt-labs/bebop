use std::collections::{BinaryHeap, HashMap};
use std::num::NonZeroU16;
use std::ops::Deref;
use std::sync::{Arc, Weak};
use std::time::{Duration, Instant};

use async_trait::async_trait;
use parking_lot::Mutex;
use tokio::sync::oneshot;

use crate::rpc::error::TransportResult;
use crate::rpc::transport::TransportProtocol;
use crate::{OwnedRecord, Record};

/// The local end of the pipe handles messages. Implementations are automatically generated from
/// bebop service definitions.
///
/// You should not implement this by hand.
#[async_trait]
pub trait ServiceHandlers<D> {
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
pub trait ServiceRequests {
    const NAME: &'static str;
}

pub trait RpcDatagram: OwnedRecord {
    /// Get the unique call ID assigned by us, the caller.
    fn call_id(&self) -> Option<NonZeroU16>;

    /// Whether this request represents a RPC call to our endpoint.
    fn is_request(&self) -> bool;

    /// Whether this request represents a response to an RPC call we made.
    /// (May or may not be an error.)
    fn is_response(&self) -> bool {
        !self.is_request()
    }

    /// Whether this datagram represents a "happy path" result.
    fn is_ok(&self) -> bool;

    /// Whether this datagram represents an error from the remote.
    fn is_err(&self) -> bool {
        !self.is_ok()
    }
}

/// A pending call which resolves into the response from the remote.
struct PendingCall<D> {
    /// The unique call ID assigned by us, the caller.
    call_id: NonZeroU16,

    tx: oneshot::Sender<TransportResult<D>>,

    /// How long this call is allowed to be pending for. If None, no timeout is specified.
    ///
    /// Warning: No timeout will lead to memory leaks if the transport does not notify the router
    /// of dropped/missing data.
    timeout: Option<Duration>,

    /// The instant at which this call was sent.
    since: Instant,
}

impl<D> PendingCall<D> {
    fn new(
        call_id: NonZeroU16,
        timeout: Option<Duration>,
    ) -> (Self, oneshot::Receiver<TransportResult<D>>) {
        let (tx, rx) = oneshot::channel::<TransportResult<D>>();
        (
            Self {
                call_id,
                tx,
                timeout,
                since: Instant::now(),
            },
            rx,
        )
    }

    fn resolve(self, value: TransportResult<D>) {
        if let Err(_) = self.tx.send(value) {
            // TODO: log this? Receiver stopped listening.
        }
    }
}

/// This is the main structure which represents information about both ends of the connection and
/// maintains the needed state to make and receive calls. This is the only struct of which an
/// instance should need to be maintained by the user.
pub struct Router<Datagram, Transport, Local, Remote> {
    /// Underlying transport
    transport: Transport,
    /// Remote service converts requests from us, so this also provides the callable RPC functions.
    remote_service: Remote,
    /// Inner router state, this may be shared with components as necessary.
    context: Arc<Mutex<RouterContext<Datagram, Local>>>,
}

pub type UnknownResponseHandler<D: RpcDatagram> = Box<dyn Fn(D)>;

pub(crate) struct RouterContext<Datagram, Local> {
    /// Callback that receives any datagrams without a call id.
    unknown_response_handler: Option<UnknownResponseHandler<Datagram>>,
    /// Local service handles requests from the remote.
    local_service: Local,
    /// Table of calls which have yet to be resolved.
    call_table: HashMap<NonZeroU16, PendingCall<Datagram>>,
    /// Min heap with next timeout as the next item
    call_timeouts: BinaryHeap<core::cmp::Reverse<CallExpiration>>,
    /// The next ID value which should be used.
    next_id: u16,
}

#[derive(Copy, Clone)]
struct CallExpiration {
    at: Instant,
    id: NonZeroU16,
}

impl PartialOrd for CallExpiration {
    fn partial_cmp(&self, other: &Self) -> Option<std::cmp::Ordering> {
        self.at.partial_cmp(&other.at)
    }
}

impl PartialEq<Self> for CallExpiration {
    fn eq(&self, other: &Self) -> bool {
        self.at.eq(&other.at)
    }
}

impl Eq for CallExpiration {}
impl Ord for CallExpiration {
    fn cmp(&self, other: &Self) -> std::cmp::Ordering {
        self.at.cmp(&other.at)
    }
}

/// Allows passthrough of function calls to the remote
impl<D, T, L, R> Deref for Router<D, T, L, R>
where
    R: ServiceRequests,
{
    type Target = R;

    fn deref(&self) -> &Self::Target {
        &self.remote_service
    }
}

impl<Datagram, Transport, Local, Remote> Router<Datagram, Transport, Local, Remote>
where
    Datagram: 'static + RpcDatagram,
    Transport: TransportProtocol<Datagram>,
    Local: 'static + ServiceHandlers<Datagram>,
    Remote: ServiceRequests,
{
    pub fn new(
        transport: Transport,
        local_service: Local,
        remote_service: Remote,
        unknown_response_handler: Option<UnknownResponseHandler<Datagram>>,
    ) -> Self {
        let mut zelf = Self {
            transport,
            remote_service,
            context: Arc::new(Mutex::new(RouterContext::new(
                local_service,
                unknown_response_handler,
            ))),
        };
        let weak_ctx = Arc::downgrade(&zelf.context);
        zelf.transport._set_handler_boxed(Box::pin(move |datagram| {
            let weak_ctx = weak_ctx.clone();
            Box::pin(async move {
                if let Some(ctx) = weak_ctx.upgrade() {
                    ctx.lock()._recv(datagram).await;
                } else {
                    // No more router, just ignore
                }
            })
        }));

        zelf
    }

    /// Get the ID which should be used for the next call that gets made.
    pub fn next_call_id(&self) -> NonZeroU16 {
        self.context.lock().next_call_id()
    }

    /// Remove pending requests which have timed out.
    pub fn clean(&self) -> usize {
        self.context.lock().clean()
    }

    // /// Send a request
    // pub async fn _send_request(&self, call_id: u16, buf: &[u8]) -> TransportResult {}
    //
    // /// Send a response to a call
    // pub async fn _send_response(&self, call_id: u16, data: &[u8]) -> TransportResult {}
    //
    // pub async fn _send_error_response(&self, call_id: u16, code: u32, msg: Option<&str>) -> TransportResult {}
    // pub async fn _send_unknown_call_response(&self, call_id: u16) -> TransportResult {}
    // pub async fn _send_invalid_sig_response(&self, call_id: u16, expected_sig: u32) -> TransportResult {}
    // pub async fn _send_call_not_supported_response(&self, call_id: u16) -> TransportResult {}
    // pub async fn _send_decode_error_response(&self, call_id: u16, info: Option<&str>) -> TransportResult {
    //     self.transport.send_decode_error_response(...).await
    // }
}

impl<D, L> RouterContext<D, L>
where
    D: RpcDatagram,
    L: ServiceHandlers<D>,
{
    fn new(local_service: L, unknown_response_handler: Option<UnknownResponseHandler<D>>) -> Self {
        Self {
            unknown_response_handler,
            local_service,
            next_id: 1,
            call_table: HashMap::new(),
            call_timeouts: BinaryHeap::new(),
        }
    }

    fn next_call_id(&mut self) -> NonZeroU16 {
        // prevent an infinite loop; if this is a problem in production, we can either increase the
        // id size OR we can stall on overflow and try again creating back pressure.
        assert!(
            self.call_table.len() < (u16::MAX as usize),
            "Call table overflow"
        );

        // zero is a "null" id
        while self.next_id == 0
            || self
                .call_table
                .contains_key(unsafe { &NonZeroU16::new_unchecked(self.next_id) })
        {
            // the value is not valid because it is 0 or is in use already
            self.next_id = self.next_id.wrapping_add(1);
        }

        // found our id, guaranteed to not be zero
        let id = unsafe { NonZeroU16::new_unchecked(self.next_id) };
        self.next_id = self.next_id.wrapping_add(1);
        id
    }

    fn clean(&mut self) -> usize {
        let mut removed = 0;
        let now = Instant::now();
        while let Some(std::cmp::Reverse(call_expiration)) = self.call_timeouts.peek().copied() {
            if call_expiration.at > now {
                break;
            }
            self.call_timeouts.pop();
            if let Some(v) = self.call_table.remove(&call_expiration.id) {
                if v.timeout.is_some() && (now - v.since) >= v.timeout.unwrap() {
                    // Removed an expired value
                    removed += 1;
                    self.call_timeouts.pop();
                } else {
                    // Oops, removed this when we should not have. This should be a rare case.
                    self.call_table.insert(v.call_id, v);
                }
            } else {
                // it no longer exists, we can move on
                self.call_timeouts.pop();
            }
        }
        removed
    }

    /// Receive a datagram and routes it.
    pub async fn _recv(&mut self, datagram: D) {
        if let Some(id) = datagram.call_id() {
            if datagram.is_request() {
                // they sent a request to our service
                self.local_service._recv_call(datagram).await;
                return;
            } else if let Some(call) = self.call_table.remove(&id) {
                // they sent a response to one of our outstanding calls
                call.resolve(Ok(datagram));
                return;
            } else {
                // we don't know the id
            }
        } else {
            // the ID was not parseable
        }

        if let Some(ref cb) = self.unknown_response_handler {
            cb(datagram)
        } else {
            // TODO: log this?
        }
    }
}
