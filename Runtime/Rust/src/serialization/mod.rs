// TODO: Create an Array wrapper and a String wrapper to enable the user to use owned types or slices
// TODO: Test "unchecked" feature

use std::collections::HashMap;
use std::convert::TryInto;
use std::hash::Hash;
use std::io::Write;

pub use error::*;
pub use fixed_sized::*;

use crate::{test_serialization, Date, Guid, SliceWrapper};
// not sure why but this is "unused"
#[allow(unused_imports)]
use crate::collection;

pub mod error;
pub mod fixed_sized;
pub mod testing;

pub type Len = u32;
/// Size of length data
pub const LEN_SIZE: usize = core::mem::size_of::<Len>();
/// Size of an enum
pub const ENUM_SIZE: usize = 4;

pub trait OwnedRecord: for<'raw> Record<'raw> {}
impl<T> OwnedRecord for T where T: for<'raw> Record<'raw> {}

pub trait OwnedSubRecord: for<'raw> SubRecord<'raw> {}
impl<T> OwnedSubRecord for T where T: for<'raw> SubRecord<'raw> {}

/// Bebop message type which can be serialized and deserialized.
pub trait Record<'raw>: SubRecord<'raw> {
    const OPCODE: Option<u32> = None;

    /// Deserialize this record
    #[inline(always)]
    fn deserialize(raw: &'raw [u8]) -> DeResult<Self> {
        Ok(Self::_deserialize_chained(raw)?.1)
    }

    /// Serialize this record. It is highly recommend to use a buffered writer.
    #[inline(always)]
    fn serialize<W: Write>(&self, dest: &mut W) -> SeResult<usize> {
        self._serialize_chained(dest)
    }

    // TODO: support async serialization
    // fn serialize_async<W: AsyncWrite>(&self, dest: &mut W) -> impl Future<Type=SeResult<usize>>;
}

/// Internal trait used to reduce the amount of code that needs to be generated.
pub trait SubRecord<'raw>: Sized {
    const MIN_SERIALIZED_SIZE: usize;
    const EXACT_SERIALIZED_SIZE: Option<usize> = None;

    /// Exact size this will be once serialized in bytes.
    ///
    /// *Warning*: call is recursive and costly to make if not needed.
    fn serialized_size(&self) -> usize;

    // TODO: test performance of this versus a generic Write
    /// Should only be called from generated code!
    /// Serialize this record. It is highly recommend to use a buffered writer.
    fn _serialize_chained<W: Write>(&self, dest: &mut W) -> SeResult<usize>;

    /// Should only be called from generated code!
    /// Deserialize this object as a sub component of a larger message. Returns a tuple of
    /// (bytes_read, deserialized_value).
    fn _deserialize_chained(raw: &'raw [u8]) -> DeResult<(usize, Self)>;
}

impl<'raw> SubRecord<'raw> for &'raw str {
    const MIN_SERIALIZED_SIZE: usize = LEN_SIZE;

    #[inline]
    fn serialized_size(&self) -> usize {
        self.len() + LEN_SIZE
    }

    fn _serialize_chained<W: Write>(&self, dest: &mut W) -> SeResult<usize> {
        let raw = self.as_bytes();
        write_len(dest, raw.len())?;
        dest.write_all(raw)?;
        Ok(LEN_SIZE + raw.len())
    }

    fn _deserialize_chained(raw: &'raw [u8]) -> DeResult<(usize, Self)> {
        let len = read_len(raw)?;
        let raw_str = &raw[LEN_SIZE..len + LEN_SIZE];
        if raw_str.len() < len {
            return Err(DeserializeError::MoreDataExpected(len - raw_str.len()));
        }
        #[cfg(not(feature = "unchecked"))]
        {
            Ok((len + LEN_SIZE, std::str::from_utf8(raw_str)?))
        }
        #[cfg(feature = "unchecked")]
        unsafe {
            Ok((len + LEN_SIZE, std::str::from_utf8_unchecked(raw_str)))
        }
    }
}

impl<'raw> SubRecord<'raw> for String {
    const MIN_SERIALIZED_SIZE: usize = <&str>::MIN_SERIALIZED_SIZE;

    #[inline]
    fn serialized_size(&self) -> usize {
        self.len() + LEN_SIZE
    }

    #[inline]
    fn _serialize_chained<W: Write>(&self, dest: &mut W) -> SeResult<usize> {
        self.as_str()._serialize_chained(dest)
    }

    #[inline]
    fn _deserialize_chained(raw: &'raw [u8]) -> DeResult<(usize, Self)> {
        <&str>::_deserialize_chained(raw).map(|(c, s)| (c, s.to_owned()))
    }
}

test_serialization!(serialization_str, &str, "some random string", 18 + LEN_SIZE);
test_serialization!(serialization_str_long, &str, "some random string that is a bit longer because I had seem some errors that seemed exclusive to longer string values.", 117 + LEN_SIZE);
test_serialization!(serialization_str_empty, &str, "", LEN_SIZE);

impl<'raw, T> SubRecord<'raw> for Vec<T>
where
    T: SubRecord<'raw>,
{
    const MIN_SERIALIZED_SIZE: usize = LEN_SIZE;

    #[inline]
    fn serialized_size(&self) -> usize {
        if let Some(size) = T::EXACT_SERIALIZED_SIZE {
            self.len() * size + LEN_SIZE
        } else {
            self.iter().fold(0, |acc, v| acc + v.serialized_size()) + LEN_SIZE
        }
    }

    fn _serialize_chained<W: Write>(&self, dest: &mut W) -> SeResult<usize> {
        write_len(dest, self.len())?;
        let mut i = LEN_SIZE;
        for v in self.iter() {
            i += v._serialize_chained(dest)?;
        }
        Ok(i)
    }

    fn _deserialize_chained(raw: &'raw [u8]) -> DeResult<(usize, Self)> {
        let len = read_len(raw)?;
        let mut i = LEN_SIZE;
        let mut v = Vec::with_capacity(len);
        for _ in 0..len {
            let (read, t) = T::_deserialize_chained(&raw[i..])?;
            i += read;
            v.push(t);
        }
        Ok((i, v))
    }
}

test_serialization!(
    serialization_vec_str,
    Vec<&str>,
    vec!["abc", "def", "ghij"],
    10 + LEN_SIZE * 4
);
test_serialization!(
    serialization_vec_layered,
    Vec<Vec<Vec<i64>>>,
    (0..4)
        .map(|_| (0..4).map(|_| (0..16).collect()).collect())
        .collect(),
    8 * 4 * 4 * 16 + LEN_SIZE + LEN_SIZE * 4 + LEN_SIZE * 4 * 4
);
test_serialization!(serialization_vec_empty_str, Vec<&str>, Vec::new(), LEN_SIZE);
test_serialization!(
    serialization_vec_i16,
    Vec<i16>,
    vec![1234, 123, 154, -194, -4234, 432],
    12 + LEN_SIZE
);
test_serialization!(serialization_vec_empty_i16, Vec<i16>, Vec::new(), LEN_SIZE);

#[cfg(feature = "sorted_maps")]
pub trait SubRecordHashMapKey<'raw>: SubRecord<'raw> + Eq + Hash + Ord {}
#[cfg(not(feature = "sorted_maps"))]
pub trait SubRecordHashMapKey<'raw>: SubRecord<'raw> + Eq + Hash {}

#[cfg(feature = "sorted_maps")]
impl<'raw, T> SubRecordHashMapKey<'raw> for T where T: SubRecord<'raw> + Eq + Hash + Ord {}
#[cfg(not(feature = "sorted_maps"))]
impl<'raw, T> SubRecordHashMapKey<'raw> for T where T: SubRecord<'raw> + Eq + Hash {}

impl<'raw, K, V> SubRecord<'raw> for HashMap<K, V>
where
    K: SubRecordHashMapKey<'raw>,
    V: SubRecord<'raw>,
{
    const MIN_SERIALIZED_SIZE: usize = LEN_SIZE;

    #[inline]
    fn serialized_size(&self) -> usize {
        match (K::EXACT_SERIALIZED_SIZE, V::EXACT_SERIALIZED_SIZE) {
            (Some(k_size), Some(v_size)) => (k_size + v_size) * self.len() + LEN_SIZE,
            (Some(k_size), None) => {
                k_size * self.len()
                    + self.values().fold(0, |acc, v| acc + v.serialized_size())
                    + LEN_SIZE
            }
            (None, Some(v_size)) => {
                self.keys().fold(0, |acc, k| acc + k.serialized_size())
                    + v_size * self.len()
                    + LEN_SIZE
            }
            (None, None) => {
                self.keys().fold(0, |acc, k| acc + k.serialized_size())
                    + self.values().fold(0, |acc, v| acc + v.serialized_size())
                    + LEN_SIZE
            }
        }
    }

    fn _serialize_chained<W: Write>(&self, dest: &mut W) -> SeResult<usize> {
        #[cfg(feature = "sorted_maps")]
        use itertools::Itertools;

        write_len(dest, self.len())?;
        let mut i = LEN_SIZE;

        #[cfg(feature = "sorted_maps")]
        let iter = self
            .iter()
            .sorted_unstable_by(|(k1, _), (k2, _)| k1.cmp(k2));
        #[cfg(not(feature = "sorted_maps"))]
        let iter = self.iter();

        for (k, v) in iter {
            i += k._serialize_chained(dest)?;
            i += v._serialize_chained(dest)?;
        }
        Ok(i)
    }

    fn _deserialize_chained(raw: &'raw [u8]) -> DeResult<(usize, Self)> {
        let len = read_len(raw)?;
        let mut i = LEN_SIZE;
        let mut m = HashMap::with_capacity(len);
        for _ in 0..len {
            let (read, k) = K::_deserialize_chained(&raw[i..])?;
            i += read;
            let (read, v) = V::_deserialize_chained(&raw[i..])?;
            i += read;
            m.insert(k, v);
        }
        Ok((i, m))
    }
}

test_serialization!(serialization_map_str_str, HashMap<&str, &str>, collection! { "k1" => "v1", "key2" => "value2" }, 14 + LEN_SIZE * 5);
test_serialization!(serialization_map_i16_str, HashMap<i16, &str>, collection! { 123 => "abc", -13 => "def", 843 => "ghij" }, 16 + LEN_SIZE * 4);
test_serialization!(serialization_map_str_i16, HashMap<&str, i16>, collection! { "abc" => 123, "def" => -13, "ghij" => 843 }, 16 + LEN_SIZE * 4);
test_serialization!(serialization_map_i16_i16, HashMap<i16, i16>, collection! { 23 => 432, -543 => 53, -43 => -12 }, 12 + LEN_SIZE);
test_serialization!(serialization_map_i16_i16_empty, HashMap<i16, i16>, HashMap::new(), LEN_SIZE);
test_serialization!(serialization_map_str_str_empty, HashMap<&str, &str>, HashMap::new(), LEN_SIZE);
test_serialization!(serialization_map_str_vec_empty_vec, HashMap<&str, Vec<i32>>, collection! {"abc" => vec![]}, LEN_SIZE * 3 + 3);

impl<'raw> SubRecord<'raw> for Guid {
    const MIN_SERIALIZED_SIZE: usize = Self::SERIALIZED_SIZE;
    const EXACT_SERIALIZED_SIZE: Option<usize> = Some(Self::SERIALIZED_SIZE);

    #[inline]
    fn serialized_size(&self) -> usize {
        Self::SERIALIZED_SIZE
    }

    #[inline]
    fn _serialize_chained<W: Write>(&self, dest: &mut W) -> SeResult<usize> {
        dest.write_all(&self.to_ms_bytes())?;
        Ok(16)
    }

    #[inline]
    fn _deserialize_chained(raw: &'raw [u8]) -> DeResult<(usize, Self)> {
        Ok((
            16,
            Guid::from_ms_bytes(
                raw[0..16]
                    .try_into()
                    .map_err(|_| DeserializeError::MoreDataExpected(16 - raw.len()))?,
            ),
        ))
    }
}

test_serialization!(
    serialization_guid,
    Guid,
    Guid::from_be_bytes([
        0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0a, 0x0b, 0x0c, 0x0d, 0x0e,
        0x0f
    ]),
    16
);

impl<'raw> SubRecord<'raw> for Date {
    const MIN_SERIALIZED_SIZE: usize = Self::SERIALIZED_SIZE;
    const EXACT_SERIALIZED_SIZE: Option<usize> = Some(Self::SERIALIZED_SIZE);

    #[inline]
    fn serialized_size(&self) -> usize {
        Self::SERIALIZED_SIZE
    }

    #[inline]
    fn _serialize_chained<W: Write>(&self, dest: &mut W) -> SeResult<usize> {
        self.to_ticks()._serialize_chained(dest)
    }

    #[inline]
    fn _deserialize_chained(raw: &'raw [u8]) -> DeResult<(usize, Self)> {
        let (read, date) = u64::_deserialize_chained(&raw)?;
        Ok((read, Date::from_ticks(date)))
    }
}

test_serialization!(serialization_date, Date, Date::from_ticks(23462356), 8);

impl<'de> SubRecord<'de> for bool {
    const MIN_SERIALIZED_SIZE: usize = 1;
    const EXACT_SERIALIZED_SIZE: Option<usize> = Some(bool::SERIALIZED_SIZE);

    #[inline]
    fn serialized_size(&self) -> usize {
        bool::SERIALIZED_SIZE
    }

    #[inline]
    fn _serialize_chained<W: Write>(&self, dest: &mut W) -> SeResult<usize> {
        dest.write_all(&[if *self { 1 } else { 0 }])?;
        Ok(1)
    }

    #[inline]
    fn _deserialize_chained(raw: &'de [u8]) -> DeResult<(usize, Self)> {
        if let Some(&b) = raw.first() {
            Ok((1, b > 0))
        } else {
            Err(DeserializeError::MoreDataExpected(1))
        }
    }
}

test_serialization!(serialization_bool_true, bool, true, 1);
test_serialization!(serialization_bool_false, bool, false, 1);

impl<'raw, T> SubRecord<'raw> for SliceWrapper<'raw, T>
where
    T: FixedSized + SubRecord<'raw>,
{
    const MIN_SERIALIZED_SIZE: usize = LEN_SIZE;

    #[inline]
    fn serialized_size(&self) -> usize {
        self.size() + LEN_SIZE
    }

    fn _serialize_chained<W: Write>(&self, dest: &mut W) -> SeResult<usize> {
        write_len(dest, self.len())?;
        match *self {
            SliceWrapper::Raw(raw) => {
                dest.write_all(&raw)?;
                Ok(raw.len() + LEN_SIZE)
            }
            SliceWrapper::Cooked(ary) => {
                #[cfg(target_endian = "big")]
                todo!();

                let b: &[u8] = unsafe {
                    std::slice::from_raw_parts(
                        ary.as_ptr() as *const u8,
                        ary.len() * core::mem::size_of::<T>(),
                    )
                };
                dest.write_all(b)?;
                Ok(b.len() + LEN_SIZE)
            }
        }
    }

    fn _deserialize_chained(raw: &'raw [u8]) -> DeResult<(usize, Self)> {
        let len = read_len(raw)?;
        let bytes = len * std::mem::size_of::<T>() + LEN_SIZE;
        if bytes > raw.len() {
            return Err(DeserializeError::MoreDataExpected(bytes - raw.len()));
        }
        Ok((
            bytes,
            if std::mem::align_of::<T>() == 1 {
                SliceWrapper::from_cooked(unsafe {
                    std::slice::from_raw_parts((&raw[LEN_SIZE..bytes]).as_ptr() as *const T, len)
                })
            } else {
                SliceWrapper::from_raw(&raw[LEN_SIZE..bytes])
            },
        ))
    }
}

test_serialization!(
    serialization_slicewrapper_u8_cooked,
    SliceWrapper<u8>,
    SliceWrapper::Cooked(&[1, 2, 3, 4, 5, 6]),
    6 + LEN_SIZE
);

#[test]
fn serialization_slicewrapper_i16_cooked() {
    let mut buf = Vec::new();
    const LEN: usize = 10 + LEN_SIZE;
    let value: SliceWrapper<i16> = SliceWrapper::Cooked(&[12, 32, 543, 652, -23]);
    assert_eq!(value._serialize_chained(&mut buf).unwrap(), LEN);
    assert_eq!(buf.len(), LEN);
    buf.extend_from_slice(&[0x05, 0x01, 0x00, 0x00, 0x13, 0x42, 0x12]);
    let (read, deserialized) = <SliceWrapper<i16>>::_deserialize_chained(&buf).unwrap();
    assert_eq!(read, LEN);
    assert_eq!(
        deserialized,
        SliceWrapper::Raw(&[12, 0, 32, 0, 31, 2, 140, 2, 233, 255])
    );
}

macro_rules! impl_record_for_num {
    ($t:ty) => {
        impl<'raw> SubRecord<'raw> for $t {
            const MIN_SERIALIZED_SIZE: usize = Self::SERIALIZED_SIZE;
            const EXACT_SERIALIZED_SIZE: Option<usize> = Some(Self::SERIALIZED_SIZE);

            #[inline]
            fn serialized_size(&self) -> usize {
                Self::SERIALIZED_SIZE
            }

            #[inline]
            fn _serialize_chained<W: Write>(&self, dest: &mut W) -> SeResult<usize> {
                dest.write_all(&self.to_le_bytes())?;
                Ok(core::mem::size_of::<$t>())
            }

            #[inline]
            fn _deserialize_chained(raw: &'raw [u8]) -> DeResult<(usize, Self)> {
                Ok((
                    core::mem::size_of::<$t>(),
                    <$t>::from_le_bytes(raw[0..core::mem::size_of::<$t>()].try_into().map_err(
                        |_| {
                            DeserializeError::MoreDataExpected(
                                core::mem::size_of::<$t>() - raw.len(),
                            )
                        },
                    )?),
                ))
            }
        }
    };
}

impl_record_for_num!(u8);
// no signed byte type at this time
impl_record_for_num!(u16);
impl_record_for_num!(i16);
impl_record_for_num!(u32);
impl_record_for_num!(i32);
impl_record_for_num!(f32);
impl_record_for_num!(u64);
impl_record_for_num!(i64);
impl_record_for_num!(f64);

/// Read a 4-byte length value from the front of the raw data.
///
/// This should only be called from within an auto-implemented deserialize function or for byte
/// hacking.
#[inline(always)]
pub fn read_len(raw: &[u8]) -> DeResult<usize> {
    Ok(Len::_deserialize_chained(&raw)?.1 as usize)
}

#[test]
fn read_len_test() {
    let buf = [23, 51, 0, 0, 2, 5];
    assert_eq!(read_len(&buf).unwrap(), 13079);
    assert_eq!(read_len(&buf[1..]).unwrap(), 33554483);
    assert_eq!(read_len(&buf[2..]).unwrap(), 84017152);
}

/// Write a 4-byte length value to the writer.
///
/// This should only be called from within an auto-implemented deserialize function or for byte
/// hacking.
#[inline(always)]
pub fn write_len<W: Write>(dest: &mut W, len: usize) -> SeResult<()> {
    if len > u32::MAX as usize {
        Err(SerializeError::LengthExceeds32Bits)
    } else {
        (len as u32)._serialize_chained(dest)?;
        Ok(())
    }
}

#[test]
fn write_len_test() {
    let mut buf = vec![0, 12, 4];
    write_len(&mut buf, 0).unwrap();
    write_len(&mut buf, 123).unwrap();
    write_len(&mut buf, 87543).unwrap();
    assert_eq!(buf.len(), 3 + LEN_SIZE * 3);
    assert_eq!(buf[3..7], [0, 0, 0, 0]);
    assert_eq!(buf[7..11], [123, 0, 0, 0]);
    assert_eq!(buf[11..], [247, 85, 1, 0]);
}
