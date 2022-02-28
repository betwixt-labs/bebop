use std::future::Future;
use std::num::NonZeroU16;
use std::pin::Pin;
use std::sync::Weak;
use std::task::{Context, Poll};
use std::time::{Duration, Instant};

use tokio::sync::oneshot;

use crate::rpc::datagram::RpcResponseHeader;
use crate::rpc::error::{RemoteRpcResponse, TransportError, TransportResult};
use crate::rpc::{Datagram, DatagramInfo, LocalRpcError, LocalRpcResponse, RouterContext};
use crate::{OwnedRecord, Record, SliceWrapper};

/// Request handle to allow sending your response to the remote.
pub struct RequestHandle {
    /// Weak reference to the context we will need to send datagrams.
    ctx: Weak<RouterContext>,
    details: CallDetails,
}

impl RequestHandle {
    pub(crate) fn new(ctx: Weak<RouterContext>, datagram: &Datagram) -> Self {
        debug_assert!(datagram.is_request(), "Must be a request");
        Self {
            ctx,
            details: CallDetails {
                timeout: datagram.timeout(),
                since: Instant::now(),
                call_id: datagram
                    .call_id()
                    .expect("Request datagrams must have a valid call ID"),
            },
        }
    }

    pub async fn send_response<'a, 'b: 'a, R: Record<'b>>(
        self,
        response: &'a LocalRpcResponse<R>,
    ) -> TransportResult {
        match response {
            Ok(record) => self.send_ok_response(record).await,
            Err(LocalRpcError::DeadlineExceded) => {
                // do nothing, no response needed as the remote should forget automatically.
                Ok(())
            }
            Err(LocalRpcError::CustomError(code, msg)) => {
                self.send_error_response(*code, Some(msg)).await
            }
            Err(LocalRpcError::CustomErrorStatic(code, msg)) => {
                let msg = if msg.is_empty() { None } else { Some(*msg) };
                self.send_error_response(*code, msg).await
            }
            Err(LocalRpcError::NotSupported) => self.send_call_not_supported_response().await,
        }
    }

    /// Send a response to a call.
    pub async fn send_ok_response<'a, 'b: 'a>(
        self,
        record: &'a impl Record<'b>,
    ) -> TransportResult {
        self.send_ok_response_raw(
            &record
                .serialize_to_vec()
                .expect("Attempted to serialize an invalid response record"),
        )
        .await
    }

    pub async fn send_ok_response_raw(self, data: &[u8]) -> TransportResult {
        if let Some(ctx) = self.ctx.upgrade() {
            ctx.send(&Datagram::RpcResponseOk {
                header: RpcResponseHeader {
                    id: self.call_id().into(),
                },
                data: SliceWrapper::Cooked(&data),
            })
            .await
        } else {
            Err(TransportError::NotConnected)
        }
    }

    pub async fn send_error_response(self, code: u32, msg: Option<&str>) -> TransportResult {
        if let Some(ctx) = self.ctx.upgrade() {
            ctx.send(&Datagram::RpcResponseErr {
                header: RpcResponseHeader {
                    id: self.call_id().into(),
                },
                code,
                info: msg.unwrap_or(""),
            })
            .await
        } else {
            Err(TransportError::NotConnected)
        }
    }

    pub async fn send_unknown_call_response(self) -> TransportResult {
        if let Some(ctx) = self.ctx.upgrade() {
            ctx.send(&Datagram::RpcResponseUnknownCall {
                header: RpcResponseHeader {
                    id: self.call_id().into(),
                },
            })
            .await
        } else {
            Err(TransportError::NotConnected)
        }
    }

    pub async fn send_invalid_sig_response(self, expected_sig: u32) -> TransportResult {
        if let Some(ctx) = self.ctx.upgrade() {
            ctx.send(&Datagram::RpcResponseInvalidSignature {
                header: RpcResponseHeader {
                    id: self.call_id().into(),
                },
                signature: expected_sig,
            })
            .await
        } else {
            Err(TransportError::NotConnected)
        }
    }

    pub async fn send_call_not_supported_response(self) -> TransportResult {
        if let Some(ctx) = self.ctx.upgrade() {
            ctx.send(&Datagram::RpcResponseCallNotSupported {
                header: RpcResponseHeader {
                    id: self.call_id().into(),
                },
            })
            .await
        } else {
            Err(TransportError::NotConnected)
        }
    }

    pub async fn send_decode_error_response(self, info: Option<&str>) -> TransportResult {
        if let Some(ctx) = self.ctx.upgrade() {
            ctx.send_decode_error_response(Some(self.call_id()), info)
                .await
        } else {
            Err(TransportError::NotConnected)
        }
    }

    pub fn call_id(&self) -> NonZeroU16 {
        self.details.call_id
    }

    pub fn duration(&self) -> Duration {
        self.details.duration()
    }

    pub fn received_at(&self) -> Instant {
        self.details.since
    }

    pub fn timeout(&self) -> Option<Duration> {
        self.details.timeout
    }

    pub fn is_expired(&self) -> bool {
        self.details.is_expired()
    }

    pub fn expires_at(&self) -> Option<Instant> {
        self.details.expires_at()
    }
}

pub(super) fn new_pending_response<R>(
    call_id: NonZeroU16,
    timeout: Option<Duration>,
) -> (Box<dyn ResponseHandle>, PendingResponse<R>)
where
    R: 'static + OwnedRecord,
{
    let (tx, rx) = oneshot::channel::<RemoteRpcResponse<R>>();
    let details = CallDetails {
        timeout,
        since: Instant::now(),
        call_id,
    };
    (
        Box::new(ResponseHandleImpl {
            tx: Some(tx),
            details,
        }),
        PendingResponse { rx, details },
    )
}

/// A pending response handle which resolves into the response from the remote. Use the trait for
/// type erasure since we can't store a bunch of different T types in one table.
///
/// Would just send `&[u8]` but doing that over a channel is not possible so we have to deserialize
/// an owned copy and _then_ send.
struct ResponseHandleImpl<T> {
    tx: Option<oneshot::Sender<RemoteRpcResponse<T>>>,
    details: CallDetails,
}

pub trait ResponseHandle: Send + Sync {
    fn call_id(&self) -> NonZeroU16;
    fn duration(&self) -> Duration;
    fn since(&self) -> Instant;
    fn timeout(&self) -> Option<Duration>;
    fn is_expired(&self) -> bool;
    fn expires_at(&self) -> Option<Instant>;
    fn resolve(&mut self, value: RemoteRpcResponse<&[u8]>);
}

/// A pending response from the remote which will resolve once you receive their reply.
pub(super) struct PendingResponse<T> {
    rx: oneshot::Receiver<RemoteRpcResponse<T>>,
    details: CallDetails,
}

impl<R: OwnedRecord + Send + Sync> ResponseHandle for ResponseHandleImpl<R> {
    fn call_id(&self) -> NonZeroU16 {
        self.details.call_id
    }

    fn duration(&self) -> Duration {
        self.details.duration()
    }

    fn since(&self) -> Instant {
        self.details.since
    }

    fn timeout(&self) -> Option<Duration> {
        self.details.timeout
    }

    fn is_expired(&self) -> bool {
        self.details.is_expired()
    }

    fn expires_at(&self) -> Option<Instant> {
        self.details.expires_at()
    }

    fn resolve(&mut self, value: RemoteRpcResponse<&[u8]>) {
        let res = value.and_then(|v| Ok(R::deserialize(v)?));
        if let Some(tx) = self.tx.take() {
            if let Err(_) = tx.send(res) {
                // TODO: log this? Receiver stopped listening.
            }
        }
    }
}

impl<T> PendingResponse<T> {
    pub fn call_id(&self) -> NonZeroU16 {
        self.details.call_id
    }

    pub fn duration(&self) -> Duration {
        self.details.duration()
    }

    pub fn since(&self) -> Instant {
        self.details.since
    }

    pub fn timeout(&self) -> Option<Duration> {
        self.details.timeout
    }

    pub fn is_expired(&self) -> bool {
        self.details.is_expired()
    }

    pub fn expires_at(&self) -> Option<Instant> {
        self.details.expires_at()
    }
}

impl<T> Future for PendingResponse<T> {
    type Output = RemoteRpcResponse<T>;

    fn poll(mut self: Pin<&mut Self>, cx: &mut Context<'_>) -> Poll<Self::Output> {
        match Pin::new(&mut self.rx).poll(cx) {
            Poll::Ready(Ok(res)) => Poll::Ready(res),
            Poll::Ready(Err(..)) if self.is_expired() => {
                Poll::Ready(Err(TransportError::Timeout.into()))
            }
            Poll::Ready(Err(..)) => Poll::Ready(Err(TransportError::CallDropped.into())),
            // timeouts are handled by the call_table and not the pending response itself
            Poll::Pending => Poll::Pending,
        }
    }
}

#[derive(Copy, Clone, Debug)]
struct CallDetails {
    /// How long this call is allowed to be pending for. If None, no timeout is specified.
    ///
    /// Warning: No timeout will lead to memory leaks if the transport does not notify the router
    /// of dropped/missing data.
    timeout: Option<Duration>,

    /// The instant at which this call was sent/received.
    since: Instant,

    /// The unique call ID assigned by us, the caller.
    call_id: NonZeroU16,
}

impl CallDetails {
    pub fn duration(&self) -> Duration {
        Instant::now() - self.since
    }

    pub fn is_expired(&self) -> bool {
        if let Some(timeout) = self.timeout {
            self.duration() >= timeout
        } else {
            false
        }
    }

    pub fn expires_at(&self) -> Option<Instant> {
        self.timeout.map(|t| self.since + t)
    }
}

// #[cfg(test)]
// mod test {
//     use crate::rpc::error::TransportError;
//     use crate::rpc::router::response::new_pending_response;
//     use std::num::NonZeroU16;
//     use std::time::Duration;
//
//     #[tokio::test]
//     async fn resolves() {
//         // happy path
//         let (handle1, pending1) =
//             new_pending_response(1.try_into().unwrap(), None);
//         let (handle2, pending2) = new_pending_response(
//             2.try_into().unwrap(),
//             Some(Duration::from_secs(10)),
//         );
//
//         let response1 = TestOwnedDatagram {
//             timeout: None,
//             call_id: NonZeroU16::new(1),
//             is_request: false,
//             is_ok: true,
//         };
//         let response2 = TestOwnedDatagram {
//             timeout: None, // responses never have timeouts, even when the request had one
//             call_id: NonZeroU16::new(2),
//             is_request: false,
//             is_ok: true,
//         };
//
//         assert_eq!(pending1.call_id(), NonZeroU16::new(1).unwrap());
//         assert_eq!(pending1.timeout(), None);
//         assert_eq!(pending2.timeout(), Some(Duration::from_secs(10)));
//         assert!(!pending1.is_expired());
//         assert!(!pending2.is_expired());
//
//         handle1.resolve(Ok(response1));
//         handle2.resolve(Ok(response2));
//
//         assert_eq!(pending1.await.unwrap(), response1);
//         assert_eq!(pending2.await.unwrap(), response2);
//     }
//
//     #[tokio::test]
//     async fn resolves_err() {
//         // when there is a transport error
//         let (handle, pending) =
//             new_pending_response(1.try_into().unwrap(), None);
//         handle.resolve(Err(TransportError::CallDropped));
//         assert!(matches!(pending.await, Err(TransportError::CallDropped)));
//     }
//
//     #[tokio::test]
//     async fn resolves_timeout() {
//         // when the handle gets dropped due to a timeout
//         let (handle, pending) = new_pending_response(
//             1.try_into().unwrap(),
//             Some(Duration::from_millis(10)),
//         );
//         assert!(!handle.is_expired());
//         assert!(!pending.is_expired());
//
//         tokio::time::sleep(Duration::from_millis(10)).await;
//         assert!(handle.is_expired());
//         assert!(pending.is_expired());
//         drop(handle);
//         let r = pending.await;
//         assert!(matches!(r, Err(TransportError::Timeout)), "{r:?}");
//     }
//
//     #[tokio::test]
//     async fn resolves_dropped() {
//         // when the handle gets dropped but it has not timed out
//         let (_, pending) = new_pending_response(1.try_into().unwrap(), None);
//         assert!(matches!(pending.await, Err(TransportError::CallDropped)));
//     }
// }