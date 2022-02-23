pub use bitflags::bitflags;

pub use serialization::*;
pub use types::*;

#[cfg(feature = "rpc")]
pub mod rpc;
mod serialization;
mod types;

pub mod prelude {
    #[cfg(feature = "rpc")]
    pub use crate::rpc::{Datagram, DatagramInfo, DynFuture, OwnedDatagram};
    pub use crate::serialization::{FixedSized, OwnedRecord, Record};
    pub use crate::types::{Date, Guid};
    pub use crate::SliceWrapper;
}
