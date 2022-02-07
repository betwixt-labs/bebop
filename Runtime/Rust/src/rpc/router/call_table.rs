use std::collections::hash_map::Entry;
use std::collections::HashMap;
use std::env;
use std::num::NonZeroU16;
use std::time::Duration;

use crate::rpc::datagram::Datagram;
use crate::rpc::router::pending_response::{new_pending_response, PendingResponse, ResponseHandle};
use crate::rpc::router::ServiceHandlers;

/// The call table which can be kept private and needs to get locked all together.
/// Expirations are handled by the context which is responsible for calling `drop_expired`.
pub(super) struct RouterCallTable<Datagram> {
    default_timeout: Option<Duration>,

    /// Table of calls which have yet to be resolved.
    call_table: HashMap<NonZeroU16, ResponseHandle<Datagram>>,

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
            call_table: Default::default(),
        }
    }
}

impl<D> RouterCallTable<D>
where
    D: Datagram,
{
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

    pub fn drop_expired(&mut self, id: NonZeroU16) {
        if let Entry::Occupied(e) = self.call_table.entry(id) {
            if e.get().is_expired() {
                e.remove();
            }
        }
    }
}
