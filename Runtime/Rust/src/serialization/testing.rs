/// Macro to make writing tests easier since most of them follow the same pattern
#[macro_export]
macro_rules! test_serialization {
    ($name:ident, $type:ty, $value:expr, $se_size:expr) => {
        #[test]
        fn $name() {
            let mut buf = Vec::new();
            let value: $type = $value;
            assert_eq!(value._serialize_chained(&mut buf).unwrap(), $se_size);
            assert_eq!(value.serialized_size(), buf.len());
            assert_eq!(buf.len(), $se_size);
            buf.extend_from_slice(&[0x05, 0x01, 0x00, 0x00, 0x13, 0x42, 0x12]);
            let (read, deserialized) = <$type>::_deserialize_chained(&buf).unwrap();
            assert_eq!(read, $se_size);
            assert_eq!(deserialized, value);
        }
    };
}

/// Macro to define collections easily. Very useful when testing.
/// Copied from https://stackoverflow.com/a/27582993/4404257
#[macro_export]
macro_rules! collection {
    // map-like
    ($($k:expr => $v:expr),* $(,)?) => {{
        use std::iter::{Iterator, IntoIterator};
        Iterator::collect(IntoIterator::into_iter([$(($k, $v),)*]))
    }};
    // set-like
    ($($v:expr),* $(,)?) => {{
        use std::iter::{Iterator, IntoIterator};
        Iterator::collect(IntoIterator::into_iter([$($v,)*]))
    }};
}
