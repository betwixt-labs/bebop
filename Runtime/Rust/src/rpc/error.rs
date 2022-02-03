use crate::SerializeError;

/// Things that could go wrong with the underlying transport, need it to be somewhat generic.
/// Things like the internet connection dying would fall under this.
pub enum TransportError {
    DatagramTooLarge,
    SerializationError(SerializeError),
    NotConnected,
    Timeout,
    Other(String),
}

pub type TransportResult<T> = Result<T, TransportError>;

/// Errors that the local may return when sending or responding to a request.
pub enum LocalRpcError {
    CustomError(u32, String),
    CustomErrorStatic(u32, &'static str),
    NotSupported,
}

/// Response type that is returned locally and will be sent to the remote.
pub type LocalRpcResponse<T> = Result<T, LocalRpcError>;

/// Errors that can be received from the remote when making a request.
pub enum RemoteRpcError {
    TransportError(TransportError),
    CustomError(u32, Option<String>),
    NotSupported,
    UnknownCall,
    InvalidSignature(u32),
    CallNotSupported,
    DecodeError(Option<String>)
}

/// A response on the channel from the remote.
pub type RemoteRpcResponse<T> = Result<T, RemoteRpcError>;
