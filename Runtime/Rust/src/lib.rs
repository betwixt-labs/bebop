#[cfg(feature = "rpc")]
mod rpc;
mod serialization;
mod types;

pub use bitflags::bitflags;
pub use serialization::*;
pub use types::*;

pub mod prelude {
    pub use crate::serialization::{FixedSized, Record};
    pub use crate::types::{Date, Guid};
    pub use crate::SliceWrapper;
}
