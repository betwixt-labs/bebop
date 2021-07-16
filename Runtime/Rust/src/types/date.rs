use std::ops::{Deref, DerefMut};
use std::time::Duration;

/// A date is stored as a 64-bit integer amount of “ticks” since 00:00:00 UTC on January 1 of year
/// 1 A.D. in the Gregorian calendar, where a “tick” is 100 nanoseconds.
#[derive(Eq, PartialEq, Ord, PartialOrd, Hash, Clone, Copy, Debug)]
pub struct Date(u64);

impl Deref for Date {
    type Target = u64;

    fn deref(&self) -> &Self::Target {
        &self.0
    }
}

impl DerefMut for Date {
    fn deref_mut(&mut self) -> &mut Self::Target {
        &mut self.0
    }
}

impl From<Date> for Duration {
    fn from(d: Date) -> Self {
        // 1 tick is 100ns
        let micros = d.0 / 10;
        let remaining_ticks = d.0 - (micros * 10);
        let nanos = remaining_ticks * 100;
        Duration::from_micros(micros) + Duration::from_nanos(nanos)
    }
}
