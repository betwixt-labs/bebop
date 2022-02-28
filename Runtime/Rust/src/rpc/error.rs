use std::error::Error;
use std::fmt::{Debug, Display, Formatter};
use std::pin::Pin;
use std::sync::Once;

use crate::{DeserializeError, SerializeError};

/// Things that could go wrong with the underlying transport, need it to be somewhat generic.
/// Things like the internet connection dying would fall under this.
#[derive(Debug)]
pub enum TransportError {
    DatagramTooLarge,
    SerializationError(SerializeError),
    DeserializationError(DeserializeError),
    NotConnected,
    Timeout,
    CallDropped,
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
    /// Custom error code with a dynamic message.
    CustomError(u32, String),
    /// Custom error code with a static message.
    CustomErrorStatic(u32, &'static str),
    /// This may be returned if the deadline provided to the function has been exceeded, however, it
    /// is also acceptable to return any other error OR okay value instead and a best-effort will be
    /// made to return it.
    ///
    /// Warning: Do not return this if there was no deadline provided to the function.
    DeadlineExceded,
    /// Indicates a given operation has not been implemented for the given server.
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
    // do we ever use this?
    TransportError(TransportError),
    CustomError(u32, Option<String>),
    NotSupported,
    UnknownCall,
    UnknownResponse,
    InvalidSignature(u32),
    CallNotSupported,
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
/// to the registered callback.
#[macro_export]
macro_rules! handle_respond_error {
    ($fut:expr, $service:literal, $function:literal, $opcode:expr, $call_id:expr) => {
        if let ::core::result::Result::Err(err) = $fut.await {
            if let ::core::option::Option::Some(cb) = ::bebop::rpc::get_on_respond_error() {
                cb(
                    ::bebop::rpc::ServiceContext {
                        service: $service,
                        function: $function,
                        opcode: $opcode,
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

pub struct ServiceContext {
    pub service: &'static str,
    pub function: &'static str,
    pub opcode: u16,
    pub call_id: u16,
}

/// Get the `on_respond_error` function pointer.
/// This should be used by generated code only.
#[inline(always)]
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
