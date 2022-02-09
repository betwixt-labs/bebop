use std::future::Future;
use std::num::NonZeroU16;
use std::pin::Pin;
use std::task::{Context, Poll};
use std::time::{Duration, Instant};

use tokio::sync::oneshot;

use crate::rpc::error::{TransportError, TransportResult};

pub(super) fn new_pending_response(
    call_id: NonZeroU16,
    timeout: Option<Duration>,
) -> (ResponseHandle, PendingResponse) {
    let (tx, rx) = oneshot::channel::<TransportResult<D>>();
    let details = CallDetails {
        timeout,
        since: Instant::now(),
        call_id,
    };
    (
        ResponseHandle { tx, details },
        PendingResponse { rx, details },
    )
}

#[derive(Copy, Clone, Debug)]
struct CallDetails {
    /// How long this call is allowed to be pending for. If None, no timeout is specified.
    ///
    /// Warning: No timeout will lead to memory leaks if the transport does not notify the router
    /// of dropped/missing data.
    timeout: Option<Duration>,

    /// The instant at which this call was sent.
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

/// A pending response handle which resolves into the response from the remote.
pub(super) struct ResponseHandle {
    tx: oneshot::Sender<TransportResult>,
    details: CallDetails,
}

impl ResponseHandle {
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

    pub fn resolve(self, value: TransportResult) {
        if let Err(_) = self.tx.send(value) {
            // TODO: log this? Receiver stopped listening.
        }
    }
}

pub(super) struct PendingResponse {
    rx: oneshot::Receiver<TransportResult>,
    details: CallDetails,
}

impl PendingResponse {
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

impl Future for PendingResponse {
    type Output = TransportResult;

    fn poll(mut self: Pin<&mut Self>, cx: &mut Context<'_>) -> Poll<Self::Output> {
        match Pin::new(&mut self.rx).poll(cx) {
            Poll::Ready(Ok(res)) => Poll::Ready(res),
            Poll::Ready(Err(..)) if self.is_expired() => Poll::Ready(Err(TransportError::Timeout)),
            Poll::Ready(Err(..)) => Poll::Ready(Err(TransportError::CallDropped)),
            // timeouts are handled by the call_table and not the pending response itself
            Poll::Pending => Poll::Pending,
        }
    }
}

#[cfg(test)]
mod test {
    use crate::rpc::error::TransportError;
    use crate::rpc::router::pending_response::new_pending_response;
    use std::num::NonZeroU16;
    use std::time::Duration;

    #[tokio::test]
    async fn resolves() {
        // happy path
        let (handle1, pending1) =
            new_pending_response(1.try_into().unwrap(), None);
        let (handle2, pending2) = new_pending_response(
            2.try_into().unwrap(),
            Some(Duration::from_secs(10)),
        );

        let response1 = TestOwnedDatagram {
            timeout: None,
            call_id: NonZeroU16::new(1),
            is_request: false,
            is_ok: true,
        };
        let response2 = TestOwnedDatagram {
            timeout: None, // responses never have timeouts, even when the request had one
            call_id: NonZeroU16::new(2),
            is_request: false,
            is_ok: true,
        };

        assert_eq!(pending1.call_id(), NonZeroU16::new(1).unwrap());
        assert_eq!(pending1.timeout(), None);
        assert_eq!(pending2.timeout(), Some(Duration::from_secs(10)));
        assert!(!pending1.is_expired());
        assert!(!pending2.is_expired());

        handle1.resolve(Ok(response1));
        handle2.resolve(Ok(response2));

        assert_eq!(pending1.await.unwrap(), response1);
        assert_eq!(pending2.await.unwrap(), response2);
    }

    #[tokio::test]
    async fn resolves_err() {
        // when there is a transport error
        let (handle, pending) =
            new_pending_response(1.try_into().unwrap(), None);
        handle.resolve(Err(TransportError::CallDropped));
        assert!(matches!(pending.await, Err(TransportError::CallDropped)));
    }

    #[tokio::test]
    async fn resolves_timeout() {
        // when the handle gets dropped due to a timeout
        let (handle, pending) = new_pending_response(
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
        assert!(matches!(r, Err(TransportError::Timeout)), "{r:?}");
    }

    #[tokio::test]
    async fn resolves_dropped() {
        // when the handle gets dropped but it has not timed out
        let (_, pending) = new_pending_response(1.try_into().unwrap(), None);
        assert!(matches!(pending.await, Err(TransportError::CallDropped)));
    }
}
