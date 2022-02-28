use crate::generated::fixedsize::*;
use bebop::{SliceWrapper, SubRecord, LEN_SIZE};

#[test]
fn validate_memory_logic() {
    assert_eq!(std::mem::align_of::<FixedSizeStruct>(), 1);
    assert_eq!(std::mem::size_of::<FixedSizeStruct>(), 13);
}

const ARRAY: [FixedSizeStruct; 3] = [
    FixedSizeStruct {
        a: 31,
        b: 896,
        c: 98623,
    },
    FixedSizeStruct {
        a: 37,
        b: 8123,
        c: 2345,
    },
    FixedSizeStruct {
        a: 123,
        b: 23947,
        c: 2631,
    },
];

#[test]
fn slice_serialization() {
    let mut buf = Vec::new();
    let s = StructWithArrayOfFixedSizeType {
        ary: SliceWrapper::Cooked(&ARRAY as &'static [FixedSizeStruct]),
    };
    assert_eq!(s._serialize_chained(&mut buf).unwrap(), 13 * 3 + LEN_SIZE);
    let (read, de) = StructWithArrayOfFixedSizeType::_deserialize_chained(&buf).unwrap();
    assert_eq!(read, 13 * 3 + LEN_SIZE);
    assert!(!de.ary.is_raw());
    assert_eq!(de, s);
    assert_eq!(de.ary.get(1).unwrap(), ARRAY[1])
}
