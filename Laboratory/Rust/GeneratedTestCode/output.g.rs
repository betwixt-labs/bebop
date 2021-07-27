//!
//! This code was generated by a tool.
//!
//!
//!   bebopc version:
//!       0.0.1-20210727-2124
//!
//!
//!   bebopc source:
//!       https://github.com/RainwayApp/bebop
//!
//!
//! Changes to this file may cause incorrect behavior and will be lost if
//! the code is regenerated.
//!

use std::io::Write as _;

#[derive(Clone, Debug, PartialEq, Default)]
pub struct InnerM {
    /// Field 1
    pub x: core::option::Option<i32>,
}

impl<'raw> bebop::SubRecord<'raw> for InnerM {
    const MIN_SERIALIZED_SIZE: usize = bebop::LEN_SIZE + 1;
    fn serialize<W: std::io::Write>(&self, dest: &mut W) -> bebop::SeResult<usize> {
        let mut buf = std::vec::Vec::new();
        if let Some(ref v) = self.x {
            buf.push(1);
            v.serialize(&mut buf)?;
        }
        buf.push(0);
        bebop::write_len(dest, buf.len())?;
        dest.write_all(&buf)?;
        Ok(buf.len() + bebop::LEN_SIZE)
    }

    fn deserialize_chained(raw: &'raw [u8]) -> bebop::DeResult<(usize, Self)> {
        let len = bebop::read_len(raw)?;
        #[cfg(not(feature = "unchecked"))]
        if len == 0 {
            return Err(bebop::DeserializeError::CorruptFrame);
        }

        if raw.len() < len + bebop::LEN_SIZE {
            return Err(bebop::DeserializeError::MoreDataExpected(len + bebop::LEN_SIZE - raw.len()));
        }
        let mut i = bebop::LEN_SIZE;
        let mut de = Self::default();

        #[cfg(not(feature = "unchecked"))]
        let mut last = 0;

        while i < len + bebop::LEN_SIZE {
            let di = raw[i];

            #[cfg(not(feature = "unchecked"))]
            if di != 0 {
                if di < last {
                    return Err(bebop::DeserializeError::CorruptFrame);
                }
                last = di;
            }

            i += 1;
            match di {
                0 => {
                    break;
                }
                1 => {
                    #[cfg(not(feature = "unchecked"))]
                    if de.x.is_some() {
                        return Err(bebop::DeserializeError::DuplicateMessageField);
                    }
                    let (read, value) = <i32>::deserialize_chained(&raw[i..])?;
                    i += read;
                    de.x = Some(value)
                }
                _ => {
                    i = len + bebop::LEN_SIZE;
                    break;
                }
            }
        }
        if i != len + bebop::LEN_SIZE {
            debug_assert!(i > len + bebop::LEN_SIZE);
            Err(bebop::DeserializeError::CorruptFrame)
        }
        else {
            Ok((i, de))
        }
    }
}

impl<'raw> bebop::Record<'raw> for InnerM {}

#[derive(Clone, Debug, PartialEq, Default)]
pub struct A {
    /// Field 1
    pub b: core::option::Option<u32>,
}

impl<'raw> bebop::SubRecord<'raw> for A {
    const MIN_SERIALIZED_SIZE: usize = bebop::LEN_SIZE + 1;
    fn serialize<W: std::io::Write>(&self, dest: &mut W) -> bebop::SeResult<usize> {
        let mut buf = std::vec::Vec::new();
        if let Some(ref v) = self.b {
            buf.push(1);
            v.serialize(&mut buf)?;
        }
        buf.push(0);
        bebop::write_len(dest, buf.len())?;
        dest.write_all(&buf)?;
        Ok(buf.len() + bebop::LEN_SIZE)
    }

    fn deserialize_chained(raw: &'raw [u8]) -> bebop::DeResult<(usize, Self)> {
        let len = bebop::read_len(raw)?;
        #[cfg(not(feature = "unchecked"))]
        if len == 0 {
            return Err(bebop::DeserializeError::CorruptFrame);
        }

        if raw.len() < len + bebop::LEN_SIZE {
            return Err(bebop::DeserializeError::MoreDataExpected(len + bebop::LEN_SIZE - raw.len()));
        }
        let mut i = bebop::LEN_SIZE;
        let mut de = Self::default();

        #[cfg(not(feature = "unchecked"))]
        let mut last = 0;

        while i < len + bebop::LEN_SIZE {
            let di = raw[i];

            #[cfg(not(feature = "unchecked"))]
            if di != 0 {
                if di < last {
                    return Err(bebop::DeserializeError::CorruptFrame);
                }
                last = di;
            }

            i += 1;
            match di {
                0 => {
                    break;
                }
                1 => {
                    #[cfg(not(feature = "unchecked"))]
                    if de.b.is_some() {
                        return Err(bebop::DeserializeError::DuplicateMessageField);
                    }
                    let (read, value) = <u32>::deserialize_chained(&raw[i..])?;
                    i += read;
                    de.b = Some(value)
                }
                _ => {
                    i = len + bebop::LEN_SIZE;
                    break;
                }
            }
        }
        if i != len + bebop::LEN_SIZE {
            debug_assert!(i > len + bebop::LEN_SIZE);
            Err(bebop::DeserializeError::CorruptFrame)
        }
        else {
            Ok((i, de))
        }
    }
}

impl<'raw> bebop::Record<'raw> for A {}

/// This branch is, too!
#[derive(Clone, Debug, PartialEq)]
pub struct B {
    pub c: bool,
}

impl<'raw> bebop::SubRecord<'raw> for B {
    const MIN_SERIALIZED_SIZE: usize =
        <bool>::MIN_SERIALIZED_SIZE;

    fn serialize<W: std::io::Write>(&self, dest: &mut W) -> bebop::SeResult<usize> {
        Ok(
            self.c.serialize(dest)?
        )
    }

    fn deserialize_chained(raw: &'raw [u8]) -> bebop::DeResult<(usize, Self)> {
        if raw.len() < Self::MIN_SERIALIZED_SIZE {
            let missing = raw.len() - Self::MIN_SERIALIZED_SIZE;
            return Err(bebop::DeserializeError::MoreDataExpected(missing));
        }

        let mut i = 0;
        let (read, v0) = <bool>::deserialize_chained(&raw[i..])?;
        i += read;

        Ok((i, Self {
            c: v0,
        }))
    }
}

impl<'raw> bebop::Record<'raw> for B {}

#[derive(Clone, Debug, PartialEq)]
pub struct C {
}

impl<'raw> bebop::SubRecord<'raw> for C {
    const MIN_SERIALIZED_SIZE: usize = 0;
    fn serialize<W: std::io::Write>(&self, dest: &mut W) -> bebop::SeResult<usize> {
        Ok(0)
    }

    fn deserialize_chained(raw: &'raw [u8]) -> bebop::DeResult<(usize, Self)> {
        if raw.len() < Self::MIN_SERIALIZED_SIZE {
            let missing = raw.len() - Self::MIN_SERIALIZED_SIZE;
            return Err(bebop::DeserializeError::MoreDataExpected(missing));
        }

        let mut i = 0;

        Ok((i, Self {
        }))
    }
}

impl<'raw> bebop::Record<'raw> for C {}

#[derive(Clone, Debug, PartialEq)]
pub struct D {
    pub msg: InnerM,
}

impl<'raw> bebop::SubRecord<'raw> for D {
    const MIN_SERIALIZED_SIZE: usize =
        <InnerM>::MIN_SERIALIZED_SIZE;

    fn serialize<W: std::io::Write>(&self, dest: &mut W) -> bebop::SeResult<usize> {
        Ok(
            self.msg.serialize(dest)?
        )
    }

    fn deserialize_chained(raw: &'raw [u8]) -> bebop::DeResult<(usize, Self)> {
        if raw.len() < Self::MIN_SERIALIZED_SIZE {
            let missing = raw.len() - Self::MIN_SERIALIZED_SIZE;
            return Err(bebop::DeserializeError::MoreDataExpected(missing));
        }

        let mut i = 0;
        let (read, v0) = <InnerM>::deserialize_chained(&raw[i..])?;
        i += read;

        Ok((i, Self {
            msg: v0,
        }))
    }
}

impl<'raw> bebop::Record<'raw> for D {}

/// This union is so documented!
#[derive(Clone, Debug, PartialEq)]
pub enum U {
    /// An unknown type which is likely defined in a newer version of the schema.
    Unknown,

    /// Discriminator 1
    A {
        /// Field 1
        b: core::option::Option<u32>,
    },

    /// This branch is, too!
    /// Discriminator 2
    B {
        c: bool,
    },

    /// Discriminator 3
    C {
    },

    /// Discriminator 4
    D {
        msg: InnerM,
    },
}

impl<'raw> bebop::SubRecord<'raw> for U {
    const MIN_SERIALIZED_SIZE: usize = bebop::LEN_SIZE + 1;

    fn serialize<W: std::io::Write>(&self, dest: &mut W) -> bebop::SeResult<usize> {
        let mut buf = std::vec::Vec::new();
        match self {
            U::Unknown => {
                return Err(bebop::SerializeError::CannotSerializeUnknownUnion);
            }
            U::A {
                b: ref _b,
            }
            => {
                buf.push(1);
                let mut msgbuf = std::vec::Vec::new();
                if let Some(ref v) = _b {
                    msgbuf.push(1);
                    v.serialize(&mut msgbuf)?;
                }
                msgbuf.push(0);
                bebop::write_len(&mut buf, msgbuf.len())?;
                &mut buf.write_all(&msgbuf)?;
            }
            U::B {
                c: ref _c,
            }
            => {
                buf.push(2);
                _c.serialize(&mut buf);
            }
            U::C {
            }
            => {
                buf.push(3);
            }
            U::D {
                msg: ref _msg,
            }
            => {
                buf.push(4);
                _msg.serialize(&mut buf);
            }
        }
        bebop::write_len(dest, buf.len());
        dest.write_all(&buf)?;
        Ok(buf.len() + bebop::LEN_SIZE)
    }

    fn deserialize_chained(raw: &'raw [u8]) -> bebop::DeResult<(usize, Self)> {
        let len = bebop::read_len(&raw)?;
        if raw.len() < len + bebop::LEN_SIZE {
            return Err(bebop::DeserializeError::MoreDataExpected(len + bebop::LEN_SIZE - raw.len()));
        }
        let mut i = bebop::LEN_SIZE + 1;
        let de = match raw[bebop::LEN_SIZE] {
            1 => {
                todo!();
            }
            2 => {
                if raw.len() - i < Self::MIN_SERIALIZED_SIZE {
                    let missing = raw.len() - Self::MIN_SERIALIZED_SIZE;
                    return Err(bebop::DeserializeError::MoreDataExpected(missing));
                }

                let (read, v0) = <bool>::deserialize_chained(&raw[i..])?;
                i += read;
                U::B {
                    c: v0,
                }
            }
            3 => {
                if raw.len() - i < Self::MIN_SERIALIZED_SIZE {
                    let missing = raw.len() - Self::MIN_SERIALIZED_SIZE;
                    return Err(bebop::DeserializeError::MoreDataExpected(missing));
                }

                U::C {
                }
            }
            4 => {
                if raw.len() - i < Self::MIN_SERIALIZED_SIZE {
                    let missing = raw.len() - Self::MIN_SERIALIZED_SIZE;
                    return Err(bebop::DeserializeError::MoreDataExpected(missing));
                }

                let (read, v0) = <InnerM>::deserialize_chained(&raw[i..])?;
                i += read;
                U::D {
                    msg: v0,
                }
            }
            _ => {
                i = len + bebop::LEN_SIZE;
                U::Unknown
            }
        };
        if !cfg!(feature = "unchecked") && i != len + bebop::LEN_SIZE {
            debug_assert!(i > len + bebop::LEN_SIZE);
            Err(bebop::DeserializeError::CorruptFrame)
        }
        else {
            Ok((i, de))
        }
    }

}


#[derive(Clone, Debug, PartialEq)]
pub struct TwoComesFirst {
    pub b: u8,
}

impl<'raw> bebop::SubRecord<'raw> for TwoComesFirst {
    const MIN_SERIALIZED_SIZE: usize =
        <u8>::MIN_SERIALIZED_SIZE;

    fn serialize<W: std::io::Write>(&self, dest: &mut W) -> bebop::SeResult<usize> {
        Ok(
            self.b.serialize(dest)?
        )
    }

    fn deserialize_chained(raw: &'raw [u8]) -> bebop::DeResult<(usize, Self)> {
        if raw.len() < Self::MIN_SERIALIZED_SIZE {
            let missing = raw.len() - Self::MIN_SERIALIZED_SIZE;
            return Err(bebop::DeserializeError::MoreDataExpected(missing));
        }

        let mut i = 0;
        let (read, v0) = <u8>::deserialize_chained(&raw[i..])?;
        i += read;

        Ok((i, Self {
            b: v0,
        }))
    }
}

impl<'raw> bebop::Record<'raw> for TwoComesFirst {}

#[derive(Clone, Debug, PartialEq)]
pub struct ThreeIsSkipped {
}

impl<'raw> bebop::SubRecord<'raw> for ThreeIsSkipped {
    const MIN_SERIALIZED_SIZE: usize = 0;
    fn serialize<W: std::io::Write>(&self, dest: &mut W) -> bebop::SeResult<usize> {
        Ok(0)
    }

    fn deserialize_chained(raw: &'raw [u8]) -> bebop::DeResult<(usize, Self)> {
        if raw.len() < Self::MIN_SERIALIZED_SIZE {
            let missing = raw.len() - Self::MIN_SERIALIZED_SIZE;
            return Err(bebop::DeserializeError::MoreDataExpected(missing));
        }

        let mut i = 0;

        Ok((i, Self {
        }))
    }
}

impl<'raw> bebop::Record<'raw> for ThreeIsSkipped {}

#[derive(Clone, Debug, PartialEq)]
pub struct OneComesLast {
}

impl<'raw> bebop::SubRecord<'raw> for OneComesLast {
    const MIN_SERIALIZED_SIZE: usize = 0;
    fn serialize<W: std::io::Write>(&self, dest: &mut W) -> bebop::SeResult<usize> {
        Ok(0)
    }

    fn deserialize_chained(raw: &'raw [u8]) -> bebop::DeResult<(usize, Self)> {
        if raw.len() < Self::MIN_SERIALIZED_SIZE {
            let missing = raw.len() - Self::MIN_SERIALIZED_SIZE;
            return Err(bebop::DeserializeError::MoreDataExpected(missing));
        }

        let mut i = 0;

        Ok((i, Self {
        }))
    }
}

impl<'raw> bebop::Record<'raw> for OneComesLast {}

#[derive(Clone, Debug, PartialEq)]
pub enum WeirdOrder {
    /// An unknown type which is likely defined in a newer version of the schema.
    Unknown,

    /// Discriminator 1
    OneComesLast {
    },

    /// Discriminator 2
    TwoComesFirst {
        b: u8,
    },

    /// Discriminator 4
    ThreeIsSkipped {
    },
}

impl<'raw> bebop::SubRecord<'raw> for WeirdOrder {
    const MIN_SERIALIZED_SIZE: usize = bebop::LEN_SIZE + 1;

    fn serialize<W: std::io::Write>(&self, dest: &mut W) -> bebop::SeResult<usize> {
        let mut buf = std::vec::Vec::new();
        match self {
            WeirdOrder::Unknown => {
                return Err(bebop::SerializeError::CannotSerializeUnknownUnion);
            }
            WeirdOrder::OneComesLast {
            }
            => {
                buf.push(1);
            }
            WeirdOrder::TwoComesFirst {
                b: ref _b,
            }
            => {
                buf.push(2);
                _b.serialize(&mut buf);
            }
            WeirdOrder::ThreeIsSkipped {
            }
            => {
                buf.push(4);
            }
        }
        bebop::write_len(dest, buf.len());
        dest.write_all(&buf)?;
        Ok(buf.len() + bebop::LEN_SIZE)
    }

    fn deserialize_chained(raw: &'raw [u8]) -> bebop::DeResult<(usize, Self)> {
        let len = bebop::read_len(&raw)?;
        if raw.len() < len + bebop::LEN_SIZE {
            return Err(bebop::DeserializeError::MoreDataExpected(len + bebop::LEN_SIZE - raw.len()));
        }
        let mut i = bebop::LEN_SIZE + 1;
        let de = match raw[bebop::LEN_SIZE] {
            1 => {
                if raw.len() - i < Self::MIN_SERIALIZED_SIZE {
                    let missing = raw.len() - Self::MIN_SERIALIZED_SIZE;
                    return Err(bebop::DeserializeError::MoreDataExpected(missing));
                }

                WeirdOrder::OneComesLast {
                }
            }
            2 => {
                if raw.len() - i < Self::MIN_SERIALIZED_SIZE {
                    let missing = raw.len() - Self::MIN_SERIALIZED_SIZE;
                    return Err(bebop::DeserializeError::MoreDataExpected(missing));
                }

                let (read, v0) = <u8>::deserialize_chained(&raw[i..])?;
                i += read;
                WeirdOrder::TwoComesFirst {
                    b: v0,
                }
            }
            4 => {
                if raw.len() - i < Self::MIN_SERIALIZED_SIZE {
                    let missing = raw.len() - Self::MIN_SERIALIZED_SIZE;
                    return Err(bebop::DeserializeError::MoreDataExpected(missing));
                }

                WeirdOrder::ThreeIsSkipped {
                }
            }
            _ => {
                i = len + bebop::LEN_SIZE;
                WeirdOrder::Unknown
            }
        };
        if !cfg!(feature = "unchecked") && i != len + bebop::LEN_SIZE {
            debug_assert!(i > len + bebop::LEN_SIZE);
            Err(bebop::DeserializeError::CorruptFrame)
        }
        else {
            Ok((i, de))
        }
    }

}


