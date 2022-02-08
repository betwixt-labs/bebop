use std::num::NonZeroU16;
use std::time::Duration;

#[cfg(test)]
pub(crate) use test::TestOwnedDatagram;

use crate::Record;

/// An abstraction around the datagram. The implementation for this will be generated automatically.
pub trait Datagram<'raw>: Record<'raw> {
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

    /// Whether this datagram represents a "happy path" result. Should always be true if `is_request`.
    fn is_ok(&self) -> bool;

    /// Whether this datagram represents an error from the remote.
    fn is_err(&self) -> bool {
        !self.is_ok()
    }
}

pub trait OwnedDatagram: for<'raw> Datagram<'raw> {}
impl<T> OwnedDatagram for T where T: for<'raw> Datagram<'raw> {}

#[cfg(test)]
mod test {
    use std::io::Write;
    use std::num::NonZeroU16;
    use std::time::Duration;

    use crate::{DeResult, Record, SeResult, SubRecord};

    use super::Datagram;

    #[derive(Debug, PartialEq, Eq, Copy, Clone)]
    pub struct TestOwnedDatagram {
        pub timeout: Option<Duration>,
        pub call_id: Option<NonZeroU16>,
        pub is_request: bool,
        pub is_ok: bool,
    }

    impl Record<'_> for TestOwnedDatagram {}

    #[cfg(test)]
    impl SubRecord<'_> for TestOwnedDatagram {
        const MIN_SERIALIZED_SIZE: usize = 0;

        fn serialized_size(&self) -> usize {
            todo!()
        }

        fn _serialize_chained<W: Write>(&self, dest: &mut W) -> SeResult<usize> {
            todo!()
        }

        fn _deserialize_chained(raw: &'_ [u8]) -> DeResult<(usize, Self)> {
            todo!()
        }
    }

    impl Datagram<'_> for TestOwnedDatagram {
        fn set_call_id(&mut self, id: NonZeroU16) {
            self.call_id = Some(id)
        }

        fn timeout(&self) -> Option<Duration> {
            self.timeout
        }

        fn call_id(&self) -> Option<NonZeroU16> {
            self.call_id
        }

        fn is_request(&self) -> bool {
            self.is_request
        }

        fn is_ok(&self) -> bool {
            self.is_ok
        }
    }

    #[derive(Debug, PartialEq, Eq, Copy, Clone)]
    pub struct TestBorrowedDatagram<'raw> {
        pub timeout: Option<Duration>,
        pub call_id: Option<NonZeroU16>,
        pub is_request: bool,
        pub is_ok: bool,
        pub raw_data: &'raw [u8],
    }

    impl<'raw> Record<'raw> for TestBorrowedDatagram<'raw> {}

    #[cfg(test)]
    impl<'raw> SubRecord<'raw> for TestBorrowedDatagram<'raw> {
        const MIN_SERIALIZED_SIZE: usize = 0;

        fn serialized_size(&self) -> usize {
            todo!()
        }

        fn _serialize_chained<W: Write>(&self, dest: &mut W) -> SeResult<usize> {
            todo!()
        }

        fn _deserialize_chained(raw: &'_ [u8]) -> DeResult<(usize, Self)> {
            todo!()
        }
    }

    impl<'raw> Datagram<'raw> for TestBorrowedDatagram<'raw> {
        fn set_call_id(&mut self, id: NonZeroU16) {
            self.call_id = Some(id)
        }

        fn timeout(&self) -> Option<Duration> {
            self.timeout
        }

        fn call_id(&self) -> Option<NonZeroU16> {
            self.call_id
        }

        fn is_request(&self) -> bool {
            self.is_request
        }

        fn is_ok(&self) -> bool {
            self.is_ok
        }
    }
}