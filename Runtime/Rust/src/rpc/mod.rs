//! Runtime code to support Bebop RPC.
//!
//! TODO: write an example of setting up RPC.

mod datagram;
pub mod error;
pub mod null_service;
mod router;
mod transport;

pub use datagram::Datagram;
pub use router::*;
pub use transport::{TransportProtocol, TransportHandler};
