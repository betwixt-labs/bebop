use std::mem::size_of;

use crate::{FixedSized, SubRecord};
use std::iter::FusedIterator;
use std::ops::Deref;
use std::ptr::slice_from_raw_parts;

/// This allows us to either wrap an existing &[T] slice to serialize it OR to store a raw byte
/// slice from an encoding and access its potentially unaligned values.
///
/// **Warning:** Creating Raw arrays manually may lead to undefined behavior, use `from_raw`.
#[derive(Copy, Clone, Debug, PartialEq)]
pub enum SliceWrapper<'a, T: FixedSized> {
    Raw(&'a [u8]),
    Cooked(&'a [T]),
}

impl<'a, T, A> From<A> for SliceWrapper<'a, T>
where
    T: FixedSized,
    A: AsRef<&'a [T]>,
{
    #[inline]
    fn from(array: A) -> Self {
        SliceWrapper::from_cooked(array.as_ref())
    }
}

impl<'a> Deref for SliceWrapper<'a, u8> {
    type Target = &'a [u8];

    #[inline]
    fn deref(&self) -> &Self::Target {
        match self {
            SliceWrapper::Raw(d) => d,
            SliceWrapper::Cooked(d) => d
        }
    }
}

impl<'a> AsRef<[u8]> for SliceWrapper<'a, u8> {
    #[inline]
    fn as_ref(&self) -> &'a [u8] {
        **self
    }
}

impl<'a, T> SliceWrapper<'a, T>
where
    T: FixedSized,
{
    /// Take in a little-endian array. Most not include size bytes since the slice already has that
    /// info.
    pub fn from_raw(bytes: &'a [u8]) -> Self {
        assert_eq!(bytes.len() % size_of::<T>(), 0);
        Self::Raw(bytes)
    }

    pub fn from_cooked(array: &'a [T]) -> Self {
        SliceWrapper::Cooked(array)
    }
}

impl<'a, T> SliceWrapper<'a, T>
where
    T: FixedSized + SubRecord<'a>,
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

impl<'a, T> SliceWrapper<'a, T>
where
    T: FixedSized,
{
    pub fn is_raw(&self) -> bool {
        matches!(self, SliceWrapper::Raw(_))
    }
}

impl<'a, T> SliceWrapper<'a, T>
where
    T: FixedSized,
{
    /// Retrieve the number of items
    #[inline]
    pub fn len(&self) -> usize {
        match *self {
            SliceWrapper::Raw(raw) => raw.len() / size_of::<T>(),
            SliceWrapper::Cooked(ary) => ary.len(),
        }
    }

    #[inline]
    pub fn is_empty(&self) -> bool {
        match *self {
            SliceWrapper::Raw(raw) => raw.is_empty(),
            SliceWrapper::Cooked(ary) => ary.is_empty(),
        }
    }

    /// Retrieve the total size of this slice's data in bytes. Does not include any extra bytes to
    /// define the length of the array so it is not the same as the serialized size.
    #[inline]
    pub fn size(&self) -> usize {
        match *self {
            SliceWrapper::Raw(raw) => raw.len(),
            SliceWrapper::Cooked(ary) => ary.len() * size_of::<T>(),
        }
    }
}

impl<'a, T> IntoIterator for SliceWrapper<'a, T>
where
    T: FixedSized + SubRecord<'a>,
{
    type Item = T;
    type IntoIter = Iter<'a, T>;

    #[inline]
    fn into_iter(self) -> Self::IntoIter {
        Iter(self, 0)
    }
}

pub struct Iter<'a, T: FixedSized>(SliceWrapper<'a, T>, usize);

impl<'a, T> Iterator for Iter<'a, T>
where
    T: FixedSized + SubRecord<'a>,
{
    type Item = T;

    fn next(&mut self) -> Option<T> {
        let r = self.0.get(self.1);
        self.1 += 1;
        r
    }
}

impl<'a, T> FusedIterator for Iter<'a, T> where T: FixedSized + SubRecord<'a> {}
impl<'a, T> ExactSizeIterator for Iter<'a, T>
where
    T: FixedSized + SubRecord<'a>,
{
    fn len(&self) -> usize {
        debug_assert!(self.1 <= self.0.len());
        self.0.len() - self.1
    }
}

#[cfg(test)]
mod test {
    use crate::{DeResult, FixedSized, SeResult, SliceWrapper, SubRecord};
    use std::convert::TryInto;
    use std::io::Write;

    #[repr(packed)]
    #[derive(Debug, Eq, PartialEq, Copy, Clone)]
    struct Fixed {
        a: u8,
        b: u64,
    }

    impl FixedSized for Fixed {}

    fn cooked_array() -> &'static [Fixed] {
        &[
            Fixed { a: 23, b: 98072396 },
            Fixed {
                a: 134,
                b: 2389502334,
            },
            Fixed { a: 73, b: 98273 },
            Fixed { a: 1, b: 59125 },
        ]
    }

    impl<'raw> SubRecord<'raw> for Fixed {
        const MIN_SERIALIZED_SIZE: usize = Self::SERIALIZED_SIZE;
        const EXACT_SERIALIZED_SIZE: Option<usize> = Some(Self::SERIALIZED_SIZE);

        fn serialized_size(&self) -> usize {
            Self::SERIALIZED_SIZE
        }

        fn _serialize_chained<W: Write>(&self, dest: &mut W) -> SeResult<usize> {
            self.a._serialize_chained(dest)?;
            self.b._serialize_chained(dest)?;
            Ok(9)
        }

        fn _deserialize_chained(raw: &'raw [u8]) -> DeResult<(usize, Self)> {
            Ok((
                9,
                Self {
                    a: raw[0],
                    b: u64::from_le_bytes((raw[1..9]).try_into().unwrap()),
                },
            ))
        }
    }

    #[test]
    fn from_raw() {
        // make sure it checks the size of the array is correct
        let s = <SliceWrapper<u16>>::from_raw(&[0x00, 0x00, 0x00, 0x00]);
        assert!(!s.is_empty());
    }

    #[test]
    #[should_panic]
    fn from_raw_err() {
        // make sure it checks the size of the array is correct
        let s = <SliceWrapper<u16>>::from_raw(&[0x00, 0x00, 0x00]);
        assert!(!s.is_empty());
    }

    #[test]
    fn get_cooked_fixed_struct() {
        let array = cooked_array();
        let s = SliceWrapper::Cooked(array);
        for i in 0..array.len() {
            assert_eq!(s.get(i).unwrap(), array[i]);
        }
    }

    #[test]
    fn get_cooked_primitive() {
        let array: &'static [u16] = &[0x0000, 0x1243, 0x8f90, 0x097a];
        let s = SliceWrapper::Cooked(array);
        for i in 0..array.len() {
            assert_eq!(s.get(i).unwrap(), array[i]);
        }
    }

    #[test]
    fn get_raw_fixed_struct() {
        // only happens for big-endian systems or systems where `repr(packed)` is not supported
        let s = <SliceWrapper<Fixed>>::Raw(&[0x01, 0x02, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00]);
        assert_eq!(s.get(0).unwrap().a, 1);
        assert_eq!(s.get(0).unwrap().b, 2);
    }

    #[test]
    fn get_raw_primitive() {
        // only happens for multi-byte values
        let s = <SliceWrapper<u16>>::Raw(&[0x01, 0x00, 0x02, 0x00]);
        assert_eq!(s.get(0).unwrap(), 1);
        assert_eq!(s.get(1).unwrap(), 2);
    }

    #[test]
    fn iter_cooked() {
        let array = cooked_array();
        let s = SliceWrapper::Cooked(array);
        for (i, v) in s.iter().enumerate() {
            assert_eq!(v, array[i]);
        }
    }

    #[test]
    fn iter_raw() {
        let s = <SliceWrapper<Fixed>>::Raw(&[
            0x01, 0x02, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x03, 0x04, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00,
        ]);
        for (i, v) in s.iter().enumerate() {
            assert_eq!(v.a, (i * 2 + 1) as u8);
            assert_eq!(v.b, (i * 2 + 2) as u64);
        }
    }

    #[test]
    fn size_cooked_struct() {
        let s = SliceWrapper::Cooked(cooked_array());
        assert_eq!(s.len(), 4);
        assert_eq!(s.size(), 9 * 4);
    }

    #[test]
    fn size_raw_struct() {
        let s = <SliceWrapper<Fixed>>::Raw(&[0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00]);
        assert_eq!(s.len(), 1);
        assert_eq!(s.size(), 9);
    }

    #[test]
    fn size_cooked_primitive() {
        let s = <SliceWrapper<u16>>::Cooked(&[0x0000, 0x0000, 0x0000]);
        assert_eq!(s.len(), 3);
        assert_eq!(s.size(), 6);
    }

    #[test]
    fn size_raw_primitive() {
        let s = <SliceWrapper<u16>>::Raw(&[0x00, 0x00, 0x00, 0x00, 0x00, 0x00]);
        assert_eq!(s.len(), 3);
        assert_eq!(s.size(), 6);
    }

    #[test]
    fn deref_u8_raw() {
        let s = <SliceWrapper<u8>>::Raw(&[0x00, 0x01, 0x04, 0x06]);
        let s2: &[u8] = *s;
        assert_eq!(s2[0], 0);
        assert_eq!(s2[1], 1);
        assert_eq!(s2[2], 4);
        assert_eq!(s2[3], 6);
    }

    #[test]
    fn deref_u8_cooked() {
        let s = <SliceWrapper<u8>>::Cooked(&[0x00, 0x01, 0x04, 0x06]);
        let s2: &[u8] = *s;
        assert_eq!(s2[0], 0);
        assert_eq!(s2[1], 1);
        assert_eq!(s2[2], 4);
        assert_eq!(s2[3], 6);
    }
}
