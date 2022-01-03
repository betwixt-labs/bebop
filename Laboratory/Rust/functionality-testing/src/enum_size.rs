use crate::generated::enum_size::SmallEnum;
use crate::generated::enum_size::HugeEnum;
use bebop::SubRecord;
use std::mem::size_of;

#[test]
fn correct_sizes() {
    assert_eq!(size_of::<SmallEnum>(), size_of::<u8>());
    assert_eq!(size_of::<HugeEnum>(), size_of::<i64>());
}
