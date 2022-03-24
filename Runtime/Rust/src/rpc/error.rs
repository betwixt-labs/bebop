use std::error::Error;
use std::fmt::{Debug, Display, Formatter};
use std::pin::Pin;
use std::sync::Once;

use crate::{DeserializeError, SerializeError};

/// Things that could go wrong with the underlying transport, need it to be somewhat generic.
/// Things like the internet connection dying would fall under this.
#[derive(Debug)]
pub enum TransportError {
    /// The datagram is too large to send. Some transports may impose different limits, but Bebop
    /// itself has a 4GiB limit on the user data as it gets serialized into a byte array.
    DatagramTooLarge,
    /// The serialization failed.
    SerializationError(SerializeError),
    /// Was unable to parse data received from the remote. This indicates an issue with the
    /// transport as signatures would catch non-agreeing structures.
    DeserializationError(DeserializeError),
    /// The transport is not connected to the remote. Ideally the transport should handle this
    /// internally rather than returning this error unless there is no way for it to reconnect.
    NotConnected,
    /// Provided timeout was reached before we received a response.
    Timeout,
    /// The request handle was dropped and we no longer care about any received responses.
    CallDropped,
    /// A custom transport-specific error.
    Other(String),
}

impl From<SerializeError> for TransportError {
    fn from(e: SerializeError) -> Self {
        Self::SerializationError(e)
    }
}

impl From<DeserializeError> for TransportError {
    fn from(e: DeserializeError) -> Self {
        Self::DeserializationError(e)
    }
}

impl Display for TransportError {
    fn fmt(&self, f: &mut Formatter<'_>) -> std::fmt::Result {
        write!(f, "{self:?}")
    }
}

impl Error for TransportError {}

pub type TransportResult<T = ()> = Result<T, TransportError>;

/// Errors that the local may return when sending or responding to a request.
#[derive(Debug)]
pub enum LocalRpcError {
    /// Custom error code with a dynamic message. You should use an Enum to keep the error codes
    /// synchronized.
    CustomError(u32, String),
    /// Custom error code with a static message. You should use an Enum to keep the error codes
    /// synchronized.
    CustomErrorStatic(u32, &'static str),
    /// This may be returned if the deadline provided to the function has been exceeded, however, it
    /// is also acceptable to return any other error OR okay value instead and a best-effort will be
    /// made to return it.
    ///
    /// Warning: Do not return this if there was no deadline provided to the function.
    DeadlineExceeded,
    /// Indicates a given operation has not been implemented for the given service. Similar to
    /// `todo!()` without causing a panic.
    NotSupported,
}

impl Display for LocalRpcError {
    fn fmt(&self, f: &mut Formatter<'_>) -> std::fmt::Result {
        write!(f, "{self:?}")
    }
}

impl Error for LocalRpcError {}

/// Response type that is returned locally and will be sent to the remote.
pub type LocalRpcResponse<T> = Result<T, LocalRpcError>;

/// Errors that can be received from the remote when making a request.
#[derive(Debug)]
pub enum RemoteRpcError {
    /// A transport error occurred whilst waiting for a response.
    TransportError(TransportError),
    /// The remote generated a custom error message. You should use an Enum to keep the error codes
    /// synchronized.
    CustomError(u32, Option<String>),
    /// The remote did not recognize the opcode. This means the remote either dropped support for
    /// this call or the remote is outdated.
    UnknownCall,
    /// Our call signature did not match that of the remote which means there is probably a version
    /// mismatch or possibly they are exposing a different service than expected.
    InvalidSignature(u32),
    /// There is no implementation for this call on the remote at this time.
    CallNotSupported,
    /// Remote was not able to decode our message.
    RemoteDecodeError(Option<String>),
}

impl From<TransportError> for RemoteRpcError {
    fn from(e: TransportError) -> Self {
        Self::TransportError(e)
    }
}

impl From<DeserializeError> for RemoteRpcError {
    fn from(e: DeserializeError) -> Self {
        Self::TransportError(e.into())
    }
}

impl From<SerializeError> for RemoteRpcError {
    fn from(e: SerializeError) -> Self {
        Self::TransportError(e.into())
    }
}

impl Display for RemoteRpcError {
    fn fmt(&self, f: &mut Formatter<'_>) -> std::fmt::Result {
        write!(f, "{self:?}")
    }
}

impl Error for RemoteRpcError {}

/// A response on the channel from the remote.
pub type RemoteRpcResponse<T> = Result<T, RemoteRpcError>;

/// Macro for the generated code to handle errors when responding to requests by forwarding
/// to the registered callback. This is mostly used by generated code, but can be useful if
/// implementing by hand. Make sure the service and function names match if using by hand
/// (including capitalization)!
///
/// This will cause any errors to be forwarded to statically registered on_respond_error function.
#[macro_export]
macro_rules! handle_respond_error {
    ($fut:expr, $service:literal, $function:literal, $call_id:expr) => {
        if let ::core::result::Result::Err(err) = $fut.await {
            if let ::core::option::Option::Some(cb) = ::bebop::rpc::get_on_respond_error() {
                cb(
                    ::bebop::rpc::ServiceContext {
                        service: $service,
                        function: $function,
                        call_id: $call_id,
                    },
                    err,
                );
            }
        }
    };
}

type OnRespondError = dyn Fn(ServiceContext, TransportError);

static mut ON_RESPOND_ERROR: Option<Pin<Box<OnRespondError>>> = None;

/// Information about where an error came from.
pub struct ServiceContext {
    /// The service name as it appears in Rust.
    pub service: &'static str,
    /// The function name as it appears in Rust.
    pub function: &'static str,
    /// Client-assigned id of this call.
    pub call_id: u16,
}

/// Get the `on_respond_error` function pointer.
/// This should be used by generated code only.
#[inline(always)]
#[doc(hidden)]
pub fn get_on_respond_error() -> Option<&'static OnRespondError> {
    unsafe {
        ON_RESPOND_ERROR.as_deref().map(|f| {
            // we can cast as static since we prevent it ever being dropped.
            std::mem::transmute::<&OnRespondError, &'static OnRespondError>(f)
        })
    }
}

/// One-time initialization. Returns true if the provided value has been set. False if it was
/// already initialized.
pub fn set_on_respond_error(cb: Pin<Box<OnRespondError>>) -> bool {
    static ON_RESPOND_ERROR_INIT: Once = Once::new();
    let mut initialized = false;
    ON_RESPOND_ERROR_INIT.call_once(|| {
        unsafe {
            ON_RESPOND_ERROR = Some(cb);
        }
        initialized = true;
    });
    initialized
}
