use std::future::Future;
use std::num::NonZeroU16;
use std::pin::Pin;
use std::task::{Context, Poll};
use std::time::{Duration, Instant};

use tokio::sync::oneshot;

use crate::rpc::error::{TransportError, TransportResult};

pub(super) fn new_pending_response<D>(
    call_id: NonZeroU16,
    timeout: Option<Duration>,
) -> (ResponseHandle<D>, PendingResponse<D>) {
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
}

/// A pending response handle which resolves into the response from the remote.
pub(super) struct ResponseHandle<D> {
    tx: oneshot::Sender<TransportResult<D>>,
    details: CallDetails,
}

impl<D> ResponseHandle<D> {
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

    pub fn resolve(self, value: TransportResult<D>) {
        if let Err(_) = self.tx.send(value) {
            // TODO: log this? Receiver stopped listening.
        }
    }
}

pub(super) struct PendingResponse<D> {
    rx: oneshot::Receiver<TransportResult<D>>,
    details: CallDetails,
}

impl<D> PendingResponse<D> {
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
}

impl<D> Future for PendingResponse<D> {
    type Output = TransportResult<D>;

    fn poll(mut self: Pin<&mut Self>, cx: &mut Context<'_>) -> Poll<Self::Output> {
        match Pin::new(&mut self.rx).poll(cx) {
            Poll::Ready(Ok(res)) => Poll::Ready(res),
            Poll::Ready(Err(..)) if self.is_expired() => Poll::Ready(Err(TransportError::Timeout)),
            Poll::Ready(Err(..)) => Poll::Ready(Err(TransportError::CallDropped)),
            Poll::Pending => Poll::Pending,
        }
    }
}
