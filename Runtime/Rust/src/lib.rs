extern crate core;

pub use bitflags::bitflags;

pub use serialization::*;
pub use types::*;

#[cfg(feature = "rpc")]
pub mod rpc;
mod serialization;
mod types;

pub mod prelude {
    #[cfg(feature = "rpc")]
    pub use crate::rpc::{OwnedDatagram, Datagram, DatagramInfo};
    pub use crate::serialization::{FixedSized, Record, OwnedRecord};
    pub use crate::types::{Date, Guid};
    pub use crate::SliceWrapper;
}
