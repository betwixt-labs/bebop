//! Runtime code to support Bebop RPC.
//!
//! TODO: write an example of setting up RPC.

use std::num::NonZeroU16;
use std::time::{Duration, Instant};

pub use datagram::{
    owned::RpcDatagram as OwnedDatagram,
    owned::{
        RpcServiceNameArgs as OwnedRpcServiceNameArgs,
        RpcServiceNameReturn as OwnedRpcServiceNameReturn,
    },
    RpcDatagram as Datagram, RpcRequestHeader as RequestHeader,
    RpcResponseHeader as ResponseHeader, RpcServiceNameArgs, RpcServiceNameReturn,
};
pub use error::*;
pub use router::*;
pub use transport::TransportProtocol;

use crate::rpc::datagram::RpcDatagram;

pub type DynFuture<'a, T = ()> =
    std::pin::Pin<Box<dyn 'a + Send + std::future::Future<Output = T>>>;

mod datagram;
mod error;
mod router;
mod transport;

/// Utility for parsing information about a datagram without having to match all cases.
pub trait DatagramInfo {
    fn call_id(&self) -> Option<NonZeroU16>;
    fn timeout(&self) -> Option<Duration>;
    fn is_ok(&self) -> bool;
    fn is_err(&self) -> bool;
    fn is_request(&self) -> bool;
    fn is_response(&self) -> bool;
    fn data(&self) -> Option<&[u8]>;
}

impl<'raw> DatagramInfo for Datagram<'raw> {
    fn call_id(&self) -> Option<NonZeroU16> {
        NonZeroU16::new(match self {
            RpcDatagram::Unknown => 0,
            RpcDatagram::RpcRequestDatagram { header, .. } => header.id,
            RpcDatagram::RpcResponseOk { header, .. } => header.id,
            RpcDatagram::RpcResponseErr { header, .. } => header.id,
            RpcDatagram::RpcResponseCallNotSupported { header, .. } => header.id,
            RpcDatagram::RpcResponseUnknownCall { header, .. } => header.id,
            RpcDatagram::RpcResponseInvalidSignature { header, .. } => header.id,
            RpcDatagram::RpcDecodeError { header, .. } => header.id,
        })
    }

    fn timeout(&self) -> Option<Duration> {
        if let RpcDatagram::RpcRequestDatagram { header, .. } = self {
            NonZeroU16::new(header.timeout).map(|t| Duration::from_secs(u16::from(t) as u64))
        } else {
            None
        }
    }

    fn is_ok(&self) -> bool {
        matches!(
            self,
            RpcDatagram::RpcRequestDatagram { .. } | RpcDatagram::RpcResponseOk { .. }
        )
    }

    fn is_err(&self) -> bool {
        !self.is_ok()
    }

    fn is_request(&self) -> bool {
        matches!(self, RpcDatagram::RpcRequestDatagram { .. })
    }

    fn is_response(&self) -> bool {
        !self.is_request()
    }

    fn data(&self) -> Option<&'raw [u8]> {
        match self {
            RpcDatagram::Unknown => None,
            RpcDatagram::RpcRequestDatagram { data, .. } => Some(**data),
            RpcDatagram::RpcResponseOk { data, .. } => Some(**data),
            RpcDatagram::RpcResponseErr { .. } => None,
            RpcDatagram::RpcResponseCallNotSupported { .. } => None,
            RpcDatagram::RpcResponseUnknownCall { .. } => None,
            RpcDatagram::RpcResponseInvalidSignature { .. } => None,
            RpcDatagram::RpcDecodeError { .. } => None,
        }
    }
}

#[repr(transparent)]
#[derive(Debug, Copy, Clone, Eq, PartialEq, Ord, PartialOrd)]
pub struct Deadline(Option<Instant>);

impl Deadline {
    pub fn has_passed(&self) -> bool {
        if let Some(v) = self.0 {
            Instant::now() > v
        } else {
            false
        }
    }
}

impl From<Option<Instant>> for Deadline {
    fn from(v: Option<Instant>) -> Self {
        Self(v)
    }
}

impl From<Instant> for Deadline {
    fn from(v: Instant) -> Self {
        Self(Some(v))
    }
}

impl From<Deadline> for Option<Instant> {
    fn from(v: Deadline) -> Self {
        v.0
    }
}

impl AsRef<Option<Instant>> for Deadline {
    fn as_ref(&self) -> &Option<Instant> {
        &self.0
    }
}

#[inline]
pub fn convert_timeout(timeout: Option<Duration>) -> Option<NonZeroU16> {
    timeout.and_then(|t| {
        let t = t.as_secs();
        debug_assert!(t < u16::MAX as u64, "Maximum timeout is 2^16-1 seconds.");
        ::core::num::NonZeroU16::new(t as u16)
    })
}

/// Less verbose way of declaring `Box::pin(async move {...})`.
#[macro_export]
macro_rules! dyn_fut {
    {$($e:stmt)*} => {
        ::std::boxed::Box::pin(async move { $(
            $e
        )* })
    };
}

/// Define a timeout value for RPC.
/// This creates an Option<Duration> value which is what RPC requests expect.
#[macro_export]
macro_rules! timeout {
    (0) => {
        timeout!(None)
    };
    (0 $t:ident) => {
        timeout!(None)
    };
    (None) => {
        None
    };
    ($dur:literal s) => {
        timeout!($dur seconds)
    };
    ($dur:literal sec) => {
        timeout!($dur seconds)
    };
    ($dur:literal seconds) => {
        Some(::core::time::Duration::from_secs($dur))
    };
    ($dur:literal m) => {
        timeout!($dur minutes)
    };
    ($dur:literal min) => {
        timeout!($dur minutes)
    };
    ($dur:literal minutes) => {
        Some(::core::time::Duration::from_secs($dur * 60))
    };
    ($dur:literal h) => {
        timeout!($dur hours)
    };
    ($dur:literal hours) => {
        Some(::core::time::Duration::from_secs($dur * 60 * 60))
    };
}

#[cfg(test)]
pub(crate) mod test_struct {
    use crate::{DeResult, FixedSized, Record, SeResult, SubRecord};
    use std::io::Write;

    #[derive(Copy, Clone, PartialEq, Eq, Debug)]
    pub struct TestStruct {
        pub v: u8,
    }
    impl FixedSized for TestStruct {}
    impl Record<'_> for TestStruct {}
    impl SubRecord<'_> for TestStruct {
        const MIN_SERIALIZED_SIZE: usize = 1;
        const EXACT_SERIALIZED_SIZE: Option<usize> = Some(1);

        fn serialized_size(&self) -> usize {
            0
        }
        fn _serialize_chained<W: Write>(&self, dest: &mut W) -> SeResult<usize> {
            dest.write_all(&[self.v])?;
            Ok(1)
        }
        fn _deserialize_chained(raw: &[u8]) -> DeResult<(usize, Self)> {
            Ok((1, Self { v: raw[0] }))
        }
    }
}
