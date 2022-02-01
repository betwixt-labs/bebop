use std::marker::PhantomData;
use std::ops::Deref;

use async_trait::async_trait;

use crate::rpc::transport::TransportProtocol;
use crate::Record;

/// The local end of the pipe handles messages. Implementations are automatically generated from
/// bebop service definitions.
///
/// You should not implement this by hand.
#[async_trait]
pub trait ServiceHandlers {
    /// Use opcode to determine which function to call, whether the signature matches,
    /// how to read the buffer, and then convert the returned values and send them as a
    /// response
    ///
    /// This should only be called by the `Router` and is not for external use.
    async fn _recv_call(&self, opcode: u16, sig: u32, call_id: u16, buf: &[u8]);
}

/// Wrappers around the process of calling remote functions. Implementations are generated from
/// bebop service definitions.
///
/// You should not implement this by hand.
pub trait ServiceRequests {
    const NAME: &'static str;
}

/// This is the main structure which represents information about both ends of the connection and
/// maintains the needed state to make and receive calls. This is the only struct of which an
/// instance should need to be maintained by the user.
pub struct Router<Datagram, Transport, Local, Remote> {
    /// Underlying transport
    transport: Transport,
    /// Local service handles requests from the remote.
    local_service: Local,
    /// Remote service converts requests from us, so this also provides the callable RPC functions.
    remote_service: Remote,
    /// Hold my datagram
    _phantom: PhantomData<Datagram>,
}

/// Allows passthrough of function calls to the remote
impl<D, T, L, R> Deref for Router<D, T, L, R>
where
    R: ServiceRequests,
{
    type Target = R;

    fn deref(&self) -> &Self::Target {
        &self.remote_service
    }
}

impl<Datagram, Transport, Local, Remote> Router<Datagram, Transport, Local, Remote>
where
    for<'dgram> Datagram: Record<'dgram>,
    Transport: TransportProtocol<Datagram>,
    Local: ServiceHandlers,
    Remote: ServiceRequests,
{
    pub fn new(transport: Transport, local_service: Local, remote_service: Remote) -> Self {
        Self {
            transport,
            local_service,
            remote_service,
            _phantom: Default::default(),
        }
    }

    /// Receive a datagram and routes it
    pub async fn _recv(&self, datagram: Datagram) {
        self.local_service._recv_call(...).await
    }

    // /// Send a request
    // pub async fn _send_request(&self, call_id: u16, buf: &[u8]) -> TransportResult {}
    //
    // /// Send a response to a call
    // pub async fn _send_response(&self, call_id: u16, data: &[u8]) -> TransportResult {}
    //
    // pub async fn _send_error_response(&self, call_id: u16, code: u32, msg: Option<&str>) -> TransportResult {}
    // pub async fn _send_unknown_call_response(&self, call_id: u16) -> TransportResult {}
    // pub async fn _send_invalid_sig_response(&self, call_id: u16, expected_sig: u32) -> TransportResult {}
    // pub async fn _send_call_not_supported_response(&self, call_id: u16) -> TransportResult {}
    // pub async fn _send_decode_error_response(&self, call_id: u16, info: Option<&str>) -> TransportResult {
    //     self.transport.send_decode_error_response(...).await
    // }
}
