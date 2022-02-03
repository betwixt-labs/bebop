use crate::OwnedRecord;
use std::num::NonZeroU16;
use std::time::Duration;

/// An abstraction around the datagram. The implementation for this will be generated automatically.
pub trait Datagram: OwnedRecord {
    /// Set the call_id for this Datagram. This will always be set by the Router before passing
    /// the datagram on to the transport.
    fn set_call_id(&mut self, id: NonZeroU16);

    /// How long this datagram is allowed to take. Any time past this and the requester will likely
    /// ignore a response to it.
    ///
    /// Response datagrams will never have a timeout, and it is optional for Request datagrams.
    fn timeout(&self) -> Option<Duration>;

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
