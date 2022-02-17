//! Runtime code to support Bebop RPC.
//!
//! TODO: write an example of setting up RPC.

use std::num::NonZeroU16;
use std::time::Duration;

pub use datagram::{
    owned::{
        RpcServiceNameArgs as OwnedRpcServiceNameArgs,
        RpcServiceNameReturn as OwnedRpcServiceNameReturn,
    },
    RpcDatagram as Datagram, owned::RpcDatagram as OwnedDatagram, RpcRequestHeader as RequestHeader,
    RpcResponseHeader as ResponseHeader, RpcServiceNameArgs, RpcServiceNameReturn,
};
pub use error::*;
pub use router::*;
pub use transport::{TransportHandler, TransportProtocol};

use crate::rpc::datagram::RpcDatagram;

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

#[inline]
pub fn convert_timeout(timeout: Option<Duration>) -> Option<NonZeroU16> {
    timeout.and_then(|t| {
        let t = t.as_secs();
        debug_assert!(t < u16::MAX as u64, "Maximum timeout is 2^16-1 seconds.");
        ::core::num::NonZeroU16::new(t as u16)
    })
}
