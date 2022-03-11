use crate::rpc::datagram::{RpcDatagram as Datagram, RpcDatagram};
use std::num::NonZeroU16;
use std::time::Duration;

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
