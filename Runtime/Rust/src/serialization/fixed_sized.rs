use crate::{Guid, Date, LEN_SIZE};

/// A trait which should be given to any type which will always take up the same amount of space
/// when serialized. This goes one step beyond the `Sized` trait which only requires it to take up
/// a set amount of space on the stack. This is saying that the total data it contains and points to
/// must always be of the exact same size for every instance.
pub trait FixedSized: Copy + Sized {
    const SERIALIZED_SIZE: usize = std::mem::size_of::<Self>();
}

impl FixedSized for Guid {}
impl FixedSized for Date {}

impl FixedSized for bool {}
impl FixedSized for char {}

impl FixedSized for i8 {}
impl FixedSized for u8 {}
impl FixedSized for i16 {}
impl FixedSized for u16 {}
impl FixedSized for i32 {}
impl FixedSized for u32 {}
impl FixedSized for f32 {}
impl FixedSized for i64 {}
impl FixedSized for u64 {}
impl FixedSized for f64 {}
impl FixedSized for usize {}
impl FixedSized for isize {}
impl FixedSized for i128 {}
impl FixedSized for u128 {}

impl<T: FixedSized, const S: usize> FixedSized for [T; S] {
    const SERIALIZED_SIZE: usize = T::SERIALIZED_SIZE * S + LEN_SIZE;
}
