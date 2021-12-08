use crate::generated::flags::MyFlags;
use bebop::SubRecord;

#[test]
fn correct_values() {
    assert_eq!(MyFlags::RED.bits(), 0x1);
    assert_eq!(MyFlags::GREEN.bits(), 0x2);
    assert_eq!(MyFlags::BLUE.bits(), 0x4);
}

#[test]
fn can_decode_flags() {
    let buf = [0x06, 0x00, 0x00, 0x0];
    let (read, flags) = MyFlags::_deserialize_chained(&buf).unwrap();
    assert_eq!(read, 4);
    assert_eq!(flags, MyFlags::GREEN | MyFlags::BLUE);
}

#[test]
fn can_encode_flags() {
    let mut buf: Vec<u8> = vec![];
    let yellow = MyFlags::RED | MyFlags::GREEN;
    let written = yellow._serialize_chained(&mut buf).unwrap();
    assert_eq!(written, 4);
    assert_eq!(buf, vec![0x03, 0x00, 0x00, 0x00]);
}
