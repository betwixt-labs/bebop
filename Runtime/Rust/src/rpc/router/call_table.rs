use std::cmp::Reverse;
use std::collections::{BinaryHeap, HashMap};
use std::env;
use std::num::NonZeroU16;
use std::time::{Duration, Instant};

use crate::rpc::datagram::Datagram;
use crate::rpc::router::pending_response::{new_pending_response, PendingResponse, ResponseHandle};
use crate::rpc::router::ServiceHandlers;

/// The call table which can be kept private and needs to get locked all together.
pub(super) struct RouterCallTable<Datagram> {
    default_timeout: Option<Duration>,

    /// Table of calls which have yet to be resolved.
    call_table: HashMap<NonZeroU16, ResponseHandle<Datagram>>,

    /// Min heap with next timeout as the next item. This makes it so we don't have to check all
    /// pending items, but it does mean we have to clean the heap even if items were handled
    /// already.
    /// TODO: replace this with tokio timeouts since tokio already does sort of thing internally
    call_timeouts: BinaryHeap<core::cmp::Reverse<CallExpiration>>,

    /// The next ID value which should be used.
    next_id: u16,
}

impl<D> Default for RouterCallTable<D> {
    fn default() -> Self {
        Self {
            default_timeout: env::var("BEBOP_RPC_DEFAULT_TIMEOUT")
                .map(|v| {
                    let dur = v.parse().expect("Invalid default timeout");
                    if dur == 0 {
                        None
                    } else {
                        Some(Duration::from_secs(dur))
                    }
                })
                .unwrap_or_default(),
            next_id: 1,
            call_table: HashMap::new(),
            call_timeouts: BinaryHeap::new(),
        }
    }
}

impl<D> RouterCallTable<D>
where
    D: Datagram,
{
    /// Cleanup any calls which have gone past their timeouts. This needs to be called every so
    /// often even with reliable transport to clean the heap.
    pub fn clean(&mut self) -> usize {
        let mut removed = 0;
        let now = Instant::now();
        while let Some(std::cmp::Reverse(call_expiration)) = self.call_timeouts.peek().copied() {
            if call_expiration.at > now {
                break;
            }
            self.call_timeouts.pop();
            if let Some(v) = self.call_table.remove(&call_expiration.id) {
                // avoiding v.is_expired to reduce calls to OS for time
                if v.timeout().is_some() && (now - v.since()) >= v.timeout().unwrap() {
                    // Removed an expired value
                    removed += 1;
                    self.call_timeouts.pop();
                } else {
                    // Oops, removed this when we should not have. This should be a rare case.
                    self.call_table.insert(v.call_id(), v);
                }
            } else {
                // it no longer exists, we can move on
                self.call_timeouts.pop();
            }
        }
        removed
    }

    /// Get the ID which should be used for the next call that gets made.
    pub fn next_call_id(&mut self) -> NonZeroU16 {
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

    /// Register a datagram before we send it.
    pub fn register(&mut self, datagram: &mut D) -> PendingResponse<D> {
        debug_assert!(datagram.is_request(), "Only requests should be registered.");
        debug_assert!(
            datagram.call_id().is_none(),
            "Datagram call ids must be set by the router."
        );

        let call_id = self.next_call_id();
        datagram.set_call_id(call_id);
        let timeout = datagram.timeout().or(self.default_timeout);
        let (handle, pending) = new_pending_response(call_id, timeout);
        self.call_table.insert(call_id, handle);
        if let Some(timeout) = timeout {
            self.call_timeouts.push(Reverse(CallExpiration {
                at: Instant::now() + timeout,
                id: call_id,
            }));
        }
        pending
    }

    /// Receive a datagram and routes it. This is used by the handler for the TransportProtocol.
    pub async fn recv(
        &mut self,
        local_service: &impl ServiceHandlers<D>,
        urh: &Option<Box<dyn Fn(D)>>,
        datagram: D,
    ) {
        if let Some(id) = datagram.call_id() {
            if datagram.is_request() {
                // they sent a request to our service
                // await this to allow for back pressure
                local_service._recv_call(datagram).await;
                return;
            } else if let Some(call) = self.call_table.remove(&id) {
                // they sent a response to one of our outstanding calls
                // no async here because they already are waiting in their own task
                call.resolve(Ok(datagram));
                return;
            } else {
                // we don't know the id
            }
        } else {
            // the ID was not parseable
        }

        if let Some(ref cb) = urh {
            cb(datagram)
        } else {
            // TODO: log this?
        }
    }
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
