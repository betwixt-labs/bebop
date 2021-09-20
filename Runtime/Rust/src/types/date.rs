use std::time::Duration;
use std::cmp::Ordering;
use std::hash::{Hash, Hasher};

/// The number of ticks between 1/1/0001 and 1/1/1970.
const TICKS_BETWEEN_EPOCHS: u64 = 621355968000000000;

/// A date is stored as a 64-bit integer amount of “ticks” since 00:00:00 UTC on January 1 of year
/// 1 A.D. in the Gregorian calendar, where a “tick” is 100 nanoseconds.
///
/// The top two bits of this value are ignored by Bebop. In .NET, they are used to specify whether a
/// date is in UTC or local to the current time zone. But in Bebop, all date-times on the wire are
/// in UTC.
#[derive(Clone, Copy, Debug)]
#[repr(transparent)]
pub struct Date(u64);

impl From<Date> for Duration {
    fn from(d: Date) -> Self {
        // 1 tick is 100ns
        let micros = d.0 / 10;
        let remaining_ticks = d.0 - (micros * 10);
        let nanos = remaining_ticks * 100;
        Duration::from_micros(micros) + Duration::from_nanos(nanos)
    }
}

impl PartialEq for Date {
    fn eq(&self, other: &Self) -> bool {
        self.to_ticks() == other.to_ticks()
    }
}
impl Eq for Date {}
impl PartialOrd for Date {
    fn partial_cmp(&self, other: &Self) -> Option<Ordering> {
        Some(self.to_ticks().cmp(&other.to_ticks()))
    }
}
impl Ord for Date {
    fn cmp(&self, other: &Self) -> Ordering {
        self.to_ticks().cmp(&other.to_ticks())
    }
}
impl Hash for Date {
    fn hash<H: Hasher>(&self, state: &mut H) {
        self.to_ticks().hash(state)
    }
}

impl Date {
    #[inline]
    pub const fn from_ticks(t: u64) -> Self {
        Self(t)
    }

    #[inline]
    pub const fn from_ticks_since_unix_epoch(t: u64) -> Self {
        Self(t + TICKS_BETWEEN_EPOCHS)
    }

    #[inline]
    pub const fn to_ticks(self) -> u64 {
        // because .NET is weird we have to remove top bits
        self.0 & 0x3fffffffffffffff
    }

    #[inline]
    pub const fn to_ticks_since_unix_epoch(self) -> u64 {
        self.to_ticks() - TICKS_BETWEEN_EPOCHS
    }

    #[inline]
    pub const fn from_micros(t: u64) -> Self {
        Self(t * 10)
    }

    #[inline]
    pub const fn from_micros_since_unix_epoch(t: u64) -> Self {
        Self::from_ticks_since_unix_epoch(t * 10)
    }

    #[inline]
    pub const fn to_micros(self) -> u64 {
        self.to_ticks() / 10
    }

    #[inline]
    pub const fn to_micros_since_unix_epoch(self) -> u64 {
        self.to_ticks_since_unix_epoch() / 10
    }

    #[inline]
    pub const fn from_millis(t: u64) -> Self {
        Date::from_micros(t * 1000)
    }

    #[inline]
    pub const fn from_millis_since_unix_epoch(t: u64) -> Self {
        Date::from_micros_since_unix_epoch(t * 1000)
    }

    #[inline]
    pub const fn to_millis(self) -> u64 {
        self.to_micros() / 1000
    }

    #[inline]
    pub const fn to_millis_since_unix_epoch(self) -> u64 {
        self.to_micros_since_unix_epoch() / 1000
    }

    #[inline]
    pub const fn from_secs(t: u64) -> Self {
        Date::from_millis(t * 1000)
    }

    #[inline]
    pub const fn from_secs_since_unix_epoch(t: u64) -> Self {
        Date::from_millis_since_unix_epoch(t * 1000)
    }

    #[inline]
    pub const fn to_secs(self) -> u64 {
        self.to_millis() / 1000
    }

    #[inline]
    pub const fn to_secs_since_unix_epoch(self) -> u64 {
        self.to_millis_since_unix_epoch() / 1000
    }

    #[inline]
    pub fn to_micros_f(self) -> f64 {
        self.0 as f64 / 10.
    }

    #[inline]
    pub fn to_micros_since_unix_epoch_f(self) -> f64 {
        self.to_ticks_since_unix_epoch() as f64 / 10.
    }

    #[inline]
    pub fn to_millis_f(self) -> f64 {
        self.to_micros_f() / 1000.
    }

    #[inline]
    pub fn to_millis_since_unix_epoch_f(self) -> f64 {
        self.to_micros_since_unix_epoch_f() / 1000.
    }

    #[inline]
    pub fn to_secs_f(self) -> f64 {
        self.to_millis_f() / 1000.
    }

    #[inline]
    pub fn to_secs_since_unix_epoch_f(self) -> f64 {
        self.to_millis_since_unix_epoch_f() / 1000.
    }
}
