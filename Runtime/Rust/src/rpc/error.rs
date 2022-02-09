use std::error::Error;
use std::fmt::{Debug, Display, Formatter};

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
    CustomError(u32, String),
    CustomErrorStatic(u32, &'static str),
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
    TransportError(TransportError),
    CustomError(u32, Option<String>),
    NotSupported,
    UnknownCall,
    InvalidSignature(u32),
    CallNotSupported,
    RemoteDecodeError(Option<String>),
}

impl From<TransportError> for RemoteRpcError {
    fn from(e: TransportError) -> Self {
        Self::TransportError(e)
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
