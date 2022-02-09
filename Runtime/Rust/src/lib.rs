extern crate core;

pub use bitflags::bitflags;

pub use serialization::*;
pub use types::*;

#[cfg(feature = "rpc")]
mod rpc;
mod serialization;
mod types;

pub mod prelude {
    #[cfg(feature = "rpc")]
    pub use crate::rpc::*;
    pub use crate::serialization::{FixedSized, Record};
    pub use crate::types::{Date, Guid};
    pub use crate::SliceWrapper;
}
