pub mod generated;

#[cfg(test)]
mod enum_size;

#[cfg(test)]
mod fixedsize;

#[cfg(test)]
mod flags;

#[cfg(test)]
mod jazz;

#[cfg(all(test, feature = "rpc"))]
pub mod rpc;
