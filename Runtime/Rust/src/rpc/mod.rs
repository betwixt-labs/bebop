//! Runtime code to support Bebop RPC.
//!
//! TODO: write an example of setting up RPC.

mod datagram;
pub mod error;
mod router;
mod transport;

pub use datagram::Datagram;
pub use router::*;
pub use transport::{TransportHandler, TransportProtocol};
