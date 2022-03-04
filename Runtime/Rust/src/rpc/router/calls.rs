use std::future::Future;
use std::marker::PhantomData;
use std::num::NonZeroU16;
use std::pin::Pin;
use std::sync::Weak;
use std::task::{Context, Poll};
use std::time::{Duration, Instant};

use tokio::sync::oneshot;

use crate::rpc::datagram::RpcResponseHeader;
use crate::rpc::error::{RemoteRpcResponse, TransportError, TransportResult};
use crate::rpc::{
    Datagram, DatagramInfo, Deadline, LocalRpcError, LocalRpcResponse, RouterContext,
};
use crate::{OwnedRecord, Record, SliceWrapper};

pub struct TypedRequestHandle<'r, R>
    where R: Record<'r>
{
    _ret_type: PhantomData<&'r R>,
    inner: RequestHandle,
}

/// Request handle to allow sending your response to the remote.
pub struct RequestHandle {
    /// Weak reference to the context we will need to send datagrams.
    ctx: Weak<RouterContext>,
    details: InnerCallDetails,
}

impl<'r, R> From<RequestHandle> for TypedRequestHandle<'r, R>
    where R: Record<'r>
{
    fn from(inner: RequestHandle) -> Self {
        Self {
            inner,
            _ret_type: PhantomData::default(),
        }
    }
}

impl<'r, R> TypedRequestHandle<'r, R>
    where R: Record<'r>
{
    pub async fn send_response(
        self,
        response: LocalRpcResponse<&R>,
    ) -> TransportResult {
        match response {
            Ok(record) => self.send_ok_response(record).await,
            Err(LocalRpcError::DeadlineExceded) => {
                // do nothing, no response needed as the remote should forget automatically.
                Ok(())
            }
            Err(LocalRpcError::CustomError(code, msg)) => {
                self.inner.send_error_response(code, Some(&msg)).await
            }
            Err(LocalRpcError::CustomErrorStatic(code, msg)) => {
                let msg = if msg.is_empty() { None } else { Some(msg) };
                self.inner.send_error_response(code, msg).await
            }
            Err(LocalRpcError::NotSupported) => self.inner.send_call_not_supported_response().await,
        }
    }

    /// Send a response to a call.
    pub async fn send_ok_response<'a, 'b: 'a>(
        self,
        record: &R,
    ) -> TransportResult {
        self.inner.send_ok_response_raw(
            &record
                .serialize_to_vec()
                .expect("Attempted to serialize an invalid response record"),
        )
            .await
    }
}

impl RequestHandle {
    pub(crate) fn new(ctx: Weak<RouterContext>, datagram: &Datagram) -> Self {
        debug_assert!(datagram.is_request(), "Must be a request");
        Self {
            ctx,
            details: InnerCallDetails {
                timeout: datagram.timeout(),
                since: Instant::now(),
                call_id: datagram
                    .call_id()
                    .expect("Request datagrams must have a valid call ID"),
            },
        }
    }

    pub async fn send_ok_response_raw(self, data: &[u8]) -> TransportResult {
        if let Some(ctx) = self.ctx.upgrade() {
            ctx.send(&Datagram::RpcResponseOk {
                header: RpcResponseHeader {
                    id: self.call_id().into(),
                },
                data: SliceWrapper::Cooked(data),
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
}

impl<'r, R> AsRef<InnerCallDetails> for TypedRequestHandle<'r, R>
    where R: Record<'r>
{
    fn as_ref(&self) -> &InnerCallDetails {
        &self.inner.details
    }
}

impl AsRef<InnerCallDetails> for RequestHandle {
    fn as_ref(&self) -> &InnerCallDetails {
        &self.details
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
    let details = InnerCallDetails {
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
    details: InnerCallDetails,
}

impl<T> AsRef<InnerCallDetails> for ResponseHandleImpl<T> {
    fn as_ref(&self) -> &InnerCallDetails {
        &self.details
    }
}

pub trait ResponseHandle: Send + Sync + CallDetails {
    fn resolve(&mut self, value: RemoteRpcResponse<&[u8]>);
}

impl<R: OwnedRecord + Send + Sync> ResponseHandle for ResponseHandleImpl<R> {
    fn resolve(&mut self, value: RemoteRpcResponse<&[u8]>) {
        let res = value.and_then(|v| Ok(R::deserialize(v)?));
        if let Some(tx) = self.tx.take() {
            if tx.send(res).is_err() {
                // TODO: log this? Receiver stopped listening.
            }
        }
    }
}

/// A pending response from the remote which will resolve once you receive their reply.
pub(super) struct PendingResponse<T> {
    rx: oneshot::Receiver<RemoteRpcResponse<T>>,
    details: InnerCallDetails,
}

impl<T> AsRef<InnerCallDetails> for PendingResponse<T> {
    fn as_ref(&self) -> &InnerCallDetails {
        &self.details
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
pub struct InnerCallDetails {
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

pub trait CallDetails {
    fn call_id(&self) -> NonZeroU16;

    fn duration(&self) -> Duration {
        Instant::now() - self.since()
    }

    fn since(&self) -> Instant;

    fn timeout(&self) -> Option<Duration>;

    fn is_expired(&self) -> bool {
        if let Some(timeout) = self.timeout() {
            self.duration() >= timeout
        } else {
            false
        }
    }

    fn expires_at(&self) -> Option<Instant> {
        self.timeout().map(|t| self.since() + t)
    }

    fn deadline(&self) -> Deadline {
        self.expires_at().into()
    }
}

impl CallDetails for InnerCallDetails {
    fn call_id(&self) -> NonZeroU16 {
        self.call_id
    }

    fn since(&self) -> Instant {
        self.since
    }

    fn timeout(&self) -> Option<Duration> {
        self.timeout
    }
}

impl<T: AsRef<InnerCallDetails>> CallDetails for T {
    fn call_id(&self) -> NonZeroU16 {
        self.as_ref().call_id()
    }

    fn since(&self) -> Instant {
        self.as_ref().since()
    }

    fn timeout(&self) -> Option<Duration> {
        self.as_ref().timeout()
    }
}

#[cfg(test)]
mod test {
    use std::num::NonZeroU16;
    use std::time::Duration;

    use crate::rpc::calls::new_pending_response;
    use crate::rpc::test_struct::TestStruct;
    use crate::rpc::{CallDetails, RemoteRpcError, TransportError};
    use crate::timeout;

    #[tokio::test]
    async fn resolves() {
        // happy path
        let (mut handle1, pending1) =
            new_pending_response::<TestStruct>(1.try_into().unwrap(), None);
        let (mut handle2, pending2) =
            new_pending_response::<TestStruct>(2.try_into().unwrap(), timeout!(10 s));

        let data1 = [65];
        let data2 = [223];

        assert_eq!(pending1.call_id(), NonZeroU16::new(1).unwrap());
        assert_eq!(pending1.timeout(), None);
        assert_eq!(pending2.timeout(), Some(Duration::from_secs(10)));
        assert!(!pending1.is_expired());
        assert!(!pending2.is_expired());

        handle1.resolve(Ok(&data1));
        handle2.resolve(Ok(&data2));

        assert_eq!(pending1.await.unwrap(), TestStruct { v: data1[0] });
        assert_eq!(pending2.await.unwrap(), TestStruct { v: data2[0] });
    }

    #[tokio::test]
    async fn resolves_err() {
        // when there is a transport error
        let (mut handle, pending) = new_pending_response::<TestStruct>(1.try_into().unwrap(), None);
        handle.resolve(Err(RemoteRpcError::TransportError(
            TransportError::CallDropped,
        )));
        assert!(matches!(
            pending.await,
            Err(RemoteRpcError::TransportError(TransportError::CallDropped))
        ));
    }

    #[tokio::test]
    async fn resolves_timeout() {
        // when the handle gets dropped due to a timeout
        let (handle, pending) = new_pending_response::<TestStruct>(
            1.try_into().unwrap(),
            Some(Duration::from_millis(10)),
        );
        assert!(!handle.is_expired());
        assert!(!pending.is_expired());

        tokio::time::sleep(Duration::from_millis(10)).await;
        assert!(handle.is_expired());
        assert!(pending.is_expired());
        drop(handle);
        let r = pending.await;
        assert!(
            matches!(
                r,
                Err(RemoteRpcError::TransportError(TransportError::Timeout))
            ),
            "{r:?}"
        );
    }

    #[tokio::test]
    async fn resolves_dropped() {
        // when the handle gets dropped but it has not timed out
        let (_, pending) = new_pending_response::<TestStruct>(1.try_into().unwrap(), None);
        assert!(matches!(
            pending.await,
            Err(RemoteRpcError::TransportError(TransportError::CallDropped))
        ));
    }
}
