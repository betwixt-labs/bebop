use std::mem::size_of;

use crate::SubRecord;
use std::iter::FusedIterator;
use std::ptr::slice_from_raw_parts;

/// This allows us to either wrap an existing &[T] slice to serialize it OR to store a raw byte
/// slice from an encoding and access its potentially unaligned values.
#[derive(Copy, Clone, Debug, PartialEq)]
pub enum SliceWrapper<'a, T> {
    Raw(&'a [u8]),
    Cooked(&'a [T]),
}

impl<'a, T, A> From<A> for SliceWrapper<'a, T>
where
    A: AsRef<&'a [T]>,
{
    #[inline]
    fn from(array: A) -> Self {
        SliceWrapper::from_cooked(array.as_ref())
    }
}

impl<'a, T> SliceWrapper<'a, T> {
    /// take in a little-endian array
    pub(crate) fn from_raw(bytes: &'a [u8]) -> Self {
        assert_eq!(bytes.len() % size_of::<T>(), 0);
        Self::Raw(bytes)
    }

    pub fn from_cooked(array: &'a [T]) -> Self {
        SliceWrapper::Cooked(array)
    }
}

impl<'a, T> SliceWrapper<'a, T>
where
    T: Sized + Copy + SubRecord<'a>,
{
    /// Retrieve a value at a given index
    pub fn get(&self, i: usize) -> Option<T> {
        match *self {
            SliceWrapper::Raw(raw) => {
                if i * size_of::<T>() + size_of::<T>() > raw.len() {
                    None
                } else {
                    let raw: &'a [u8] = unsafe {
                        &*slice_from_raw_parts(
                            raw.as_ptr().offset((i * size_of::<T>()) as isize),
                            size_of::<T>(),
                        )
                    };
                    Some(T::_deserialize_chained(raw).map(|(_, v)| v).unwrap())
                }
            }
            SliceWrapper::Cooked(ary) => ary.get(i).copied(),
        }
    }

    pub fn iter(&self) -> Iter<'a, T> {
        self.into_iter()
    }
}

impl<'a, T> SliceWrapper<'a, T> {
    pub fn is_raw(&self) -> bool {
        matches!(self, SliceWrapper::Raw(_))
    }
}

impl<'a, T> SliceWrapper<'a, T>
where
    T: Sized,
{
    /// Retrieve the number of items
    pub fn len(&self) -> usize {
        match *self {
            SliceWrapper::Raw(raw) => raw.len() / size_of::<T>(),
            SliceWrapper::Cooked(ary) => ary.len(),
        }
    }

    pub fn is_empty(&self) -> bool {
        match *self {
            SliceWrapper::Raw(raw) => raw.is_empty(),
            SliceWrapper::Cooked(ary) => ary.is_empty(),
        }
    }

    /// Retrieve the total size of this slice in bytes
    pub fn size(&self) -> usize {
        match *self {
            SliceWrapper::Raw(raw) => raw.len(),
            SliceWrapper::Cooked(ary) => ary.len() * size_of::<T>(),
        }
    }
}

impl<'a, T> IntoIterator for SliceWrapper<'a, T>
where
    T: Sized + Copy + SubRecord<'a>,
{
    type Item = T;
    type IntoIter = Iter<'a, T>;

    #[inline]
    fn into_iter(self) -> Self::IntoIter {
        Iter(self, 0)
    }
}

pub struct Iter<'a, T>(SliceWrapper<'a, T>, usize);

impl<'a, T> Iterator for Iter<'a, T>
where
    T: Sized + Copy + SubRecord<'a>,
{
    type Item = T;

    fn next(&mut self) -> Option<T> {
        let r = self.0.get(self.1);
        self.1 += 1;
        r
    }
}

impl<'a, T> FusedIterator for Iter<'a, T> where T: Sized + Copy + SubRecord<'a> {}
impl<'a, T> ExactSizeIterator for Iter<'a, T>
where
    T: Sized + Copy + SubRecord<'a>,
{
    fn len(&self) -> usize {
        debug_assert!(self.1 <= self.0.len());
        self.0.len() - self.1
    }
}
