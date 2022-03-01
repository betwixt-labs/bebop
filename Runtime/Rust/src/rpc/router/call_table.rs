use std::collections::hash_map::Entry;
use std::collections::HashMap;
use std::env;
use std::num::NonZeroU16;
use std::time::Duration;

use crate::rpc::datagram::RpcDatagram;
use crate::rpc::error::RemoteRpcError;
use crate::rpc::router::calls::{new_pending_response, PendingResponse, ResponseHandle};
use crate::rpc::router::context::UnknownResponseHandler;
use crate::rpc::{Datagram, DatagramInfo};
use crate::OwnedRecord;

/// The call table which can be kept private and needs to get locked all together.
/// Expirations are handled by the context which is responsible for calling `drop_expired`.
pub(super) struct RouterCallTable {
    default_timeout: Option<Duration>,

    /// Table of calls which have yet to be resolved.
    call_table: HashMap<NonZeroU16, Box<dyn ResponseHandle>>,

    /// The next ID value which should be used.
    next_id: u16,
}

impl Default for RouterCallTable {
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

impl RouterCallTable {
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

    /// Register a datagram before we send it. This will set the call_id.
    pub fn register<R>(&mut self, datagram: &Datagram) -> PendingResponse<R>
    where
        R: 'static + OwnedRecord,
    {
        debug_assert!(datagram.is_request(), "Only requests should be registered.");
        let call_id = datagram
            .call_id()
            .expect("Datagram call ids must be set by the router.");

        let timeout = datagram.timeout().or(self.default_timeout);
        let (handle, pending) = new_pending_response(call_id, timeout);

        self.call_table.insert(call_id, handle);
        pending
    }

    /// Receive a datagram and routes it. This is used by the handler for the TransportProtocol.
    pub fn resolve(&mut self, urh: &Option<UnknownResponseHandler>, datagram: &Datagram) {
        debug_assert!(datagram.is_response(), "Only responses should be resolved.");
        let v = match datagram {
            RpcDatagram::Unknown => None,
            RpcDatagram::RpcResponseOk { header, data } => Some((header.id, Ok(**data))),
            RpcDatagram::RpcResponseErr {
                header, code, info, ..
            } => Some((
                header.id,
                Err(RemoteRpcError::CustomError(
                    *code,
                    if info.is_empty() {
                        None
                    } else {
                        Some((*info).into())
                    },
                )),
            )),
            RpcDatagram::RpcResponseCallNotSupported { header, .. } => {
                Some((header.id, Err(RemoteRpcError::CallNotSupported)))
            }
            RpcDatagram::RpcResponseUnknownCall { header, .. } => {
                Some((header.id, Err(RemoteRpcError::UnknownResponse)))
            }
            RpcDatagram::RpcResponseInvalidSignature {
                header, signature, ..
            } => Some((header.id, Err(RemoteRpcError::InvalidSignature(*signature)))),
            RpcDatagram::RpcDecodeError { header, info, .. } => Some((
                header.id,
                Err(RemoteRpcError::RemoteDecodeError(if info.is_empty() {
                    None
                } else {
                    Some((*info).into())
                })),
            )),
            RpcDatagram::RpcRequestDatagram { .. } => unreachable!(),
        }
        .map(|(id, res)| (NonZeroU16::new(id), res));

        if let Some((Some(id), res)) = v {
            if let Some(mut call) = self.call_table.remove(&id) {
                call.resolve(res);
            }
        } else if let Some(cb) = urh {
            cb(datagram)
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

#[cfg(test)]
mod test {
    use std::io::Write;
    use std::num::NonZeroU16;
    use std::time::Duration;

    use crate::prelude::Datagram;
    use crate::rpc::calls::{CallDetails, new_pending_response};
    use crate::rpc::datagram::{RpcRequestHeader, RpcResponseHeader};
    use crate::rpc::error::{RemoteRpcError, TransportError};
    use crate::rpc::DatagramInfo;
    use crate::{DeResult, FixedSized, Record, SeResult, SliceWrapper, SubRecord};

    use super::RouterCallTable;

    #[derive(Copy, Clone)]
    struct TestStruct {
        v: u8,
    }
    impl FixedSized for TestStruct {}
    impl Record<'_> for TestStruct {}
    impl SubRecord<'_> for TestStruct {
        const MIN_SERIALIZED_SIZE: usize = 0;
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

    #[test]
    fn gets_next_id() {
        let mut ct = RouterCallTable::default();
        assert_eq!(ct.next_call_id().get(), 1u16);
        assert_eq!(ct.next_call_id().get(), 2u16);
        ct.next_id = u16::MAX;
        assert_eq!(ct.next_call_id().get(), u16::MAX);
        assert_eq!(ct.next_call_id().get(), 1u16);
    }

    #[test]
    fn get_next_id_avoids_duplicates() {
        let mut ct = RouterCallTable::default();
        let (h1, _p1) = new_pending_response::<TestStruct>(1.try_into().unwrap(), None);
        ct.call_table.insert(h1.call_id(), h1);
        let (h2, _p2) = new_pending_response::<TestStruct>(2.try_into().unwrap(), None);
        ct.call_table.insert(h2.call_id(), h2);
        ct.next_id = u16::MAX;
        assert_eq!(ct.next_call_id().get(), u16::MAX);
        assert_eq!(ct.next_call_id().get(), 3u16);
    }

    #[test]
    fn registers_requests() {
        let mut ct = RouterCallTable::default();
        let timeout = Some(Duration::from_secs(10));
        let data = vec![];
        let d = Datagram::RpcRequestDatagram {
            header: RpcRequestHeader {
                id: ct.next_call_id().get(),
                timeout: timeout.unwrap().as_secs() as u16,
                signature: 0,
            },
            opcode: 0,
            data: SliceWrapper::Cooked(&data),
        };
        let pending = ct.register::<TestStruct>(&d);
        assert_eq!(pending.timeout(), timeout);
        assert_eq!(d.call_id(), NonZeroU16::new(1));
        assert_eq!(pending.call_id(), d.call_id().unwrap());
        assert_eq!(ct.next_id, 2);
        assert!(ct.call_table.contains_key(&(1.try_into().unwrap())));
    }

    #[tokio::test]
    async fn forwards_responses() {
        let mut ct = RouterCallTable::default();
        let data_a = vec![12];
        let id = ct.next_call_id().get();
        let request = Datagram::RpcRequestDatagram {
            header: RpcRequestHeader {
                id,
                timeout: 0,
                signature: 0,
            },
            opcode: 0,
            data: SliceWrapper::Cooked(&data_a),
        };
        let pending = ct.register::<TestStruct>(&request);
        let data_b = vec![15];
        let response = Datagram::RpcResponseOk {
            header: RpcResponseHeader { id },
            data: SliceWrapper::Cooked(&data_b),
        };
        ct.resolve(&None, &response);
        let d = pending.await.unwrap();
        assert_eq!(d.v, 15);
        assert!(!ct.call_table.contains_key(&id.try_into().unwrap()));
    }

    #[tokio::test]
    async fn drops_expired_entry() {
        let mut ct = RouterCallTable::default();
        let data = vec![];
        let id = NonZeroU16::new(1).unwrap();
        let request = Datagram::RpcRequestDatagram {
            header: RpcRequestHeader {
                timeout: 1,
                id: id.get(),
                signature: 0,
            },
            opcode: 0,
            data: SliceWrapper::Cooked(&data),
        };
        let pending = ct.register::<TestStruct>(&request);
        tokio::time::sleep(Duration::from_secs(1)).await;

        ct.drop_expired(id);
        assert!(!ct.call_table.contains_key(&id));
        assert!(matches!(
            pending.await,
            Err(RemoteRpcError::TransportError(TransportError::Timeout))
        ));
    }
}
