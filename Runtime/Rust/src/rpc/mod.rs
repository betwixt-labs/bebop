//! Runtime code to support Bebop RPC. Checkout https://github.com/RainwayApp/bebop/wiki/rpc for
//! more information.

use std::num::NonZeroU16;
use std::time::Duration;

pub use bebop_handler_macro::handlers;
pub use datagram::{
    owned::RpcDatagram as OwnedDatagram,
    owned::{
        RpcServiceNameArgs as OwnedRpcServiceNameArgs,
        RpcServiceNameReturn as OwnedRpcServiceNameReturn,
    },
    RpcDatagram as Datagram, RpcRequestHeader as RequestHeader,
    RpcResponseHeader as ResponseHeader, RpcServiceNameArgs, RpcServiceNameReturn,
};
pub use datagram_info::DatagramInfo;
pub use deadlines::Deadline;
pub use error::*;
pub use router::*;
pub use transport::TransportProtocol;

pub type DynFuture<'a, T = ()> =
    std::pin::Pin<Box<dyn 'a + Send + std::future::Future<Output = T>>>;

mod datagram;
mod datagram_info;
mod deadlines;
mod error;
mod router;
mod transport;

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

#[cfg(test)]
pub(crate) mod test_struct {
    use std::io::Write;

    use crate::{DeResult, FixedSized, Record, SeResult, SubRecord};

    #[derive(Copy, Clone, PartialEq, Eq, Debug)]
    pub(crate) struct TestStruct {
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
