//!
//! This code was generated by a tool.
//!
//!
//!   bebopc version:
//!       0.0.1-20210722-2330
//!
//!
//!   bebopc source:
//!       https://github.com/RainwayApp/bebop
//!
//!
//! Changes to this file may cause incorrect behavior and will be lost if
//! the code is regenerated.
//!

use bebop::{Record as _, SubRecord as _};

#[derive(Clone, Debug, PartialEq)]
pub struct BasicTypes<'raw> {
    pub a_bool: bool,
    pub a_byte: u8,
    pub a_int16: i16,
    pub a_uint16: u16,
    pub a_int32: i32,
    pub a_uint32: u32,
    pub a_int64: i64,
    pub a_uint64: u64,
    pub a_float32: f32,
    pub a_float64: f64,
    pub a_string: &'raw str,
    pub a_guid: bebop::Guid,
    pub a_date: bebop::Date,
}


#[derive(Clone, Debug, PartialEq)]
pub struct BasicArrays<'raw> {
    pub a_bool: &'raw [bool],
    pub a_byte: &'raw [u8],
    pub a_int16: bebop::PrimitiveMultiByteArray<'raw, i16>,
    pub a_uint16: bebop::PrimitiveMultiByteArray<'raw, u16>,
    pub a_int32: bebop::PrimitiveMultiByteArray<'raw, i32>,
    pub a_uint32: bebop::PrimitiveMultiByteArray<'raw, u32>,
    pub a_int64: bebop::PrimitiveMultiByteArray<'raw, i64>,
    pub a_uint64: bebop::PrimitiveMultiByteArray<'raw, u64>,
    pub a_float32: bebop::PrimitiveMultiByteArray<'raw, f32>,
    pub a_float64: bebop::PrimitiveMultiByteArray<'raw, f64>,
    pub a_string: std::vec::Vec<&'raw str>,
    pub a_guid: &'raw [bebop::Guid],
}


#[derive(Clone, Debug, PartialEq)]
pub struct TestInt32Array<'raw> {
    pub a: bebop::PrimitiveMultiByteArray<'raw, i32>,
}


#[derive(Clone, Debug, PartialEq)]
pub struct ArrayOfStrings<'raw> {
    pub strings: std::vec::Vec<&'raw str>,
}


#[derive(Clone, Debug, PartialEq)]
pub struct StructOfStructs<'raw> {
    pub basic_types: BasicTypes<'raw>,
    pub basic_arrays: BasicArrays<'raw>,
}


#[derive(Clone, Debug, PartialEq)]
pub struct EmptyStruct {
}


#[derive(Clone, Debug, PartialEq)]
pub struct OpcodeStruct {
    pub x: i32,
}


#[derive(Clone, Debug, PartialEq)]
pub struct ShouldNotHaveLifetime<'raw> {
    pub v: std::vec::Vec<OpcodeStruct>,
}


