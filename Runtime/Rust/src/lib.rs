mod serialization;
mod types;

pub use serialization::*;
pub use types::*;

pub mod prelude {
    pub use crate::serialization::Record;
    pub use crate::SliceWrapper;
    pub use crate::types::{Date, Guid};
}

#[cfg(feature = "cargo")]
pub mod cargo;
