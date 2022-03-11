use std::time::Instant;

#[repr(transparent)]
#[derive(Debug, Copy, Clone, Eq, PartialEq, Ord, PartialOrd)]
pub struct Deadline(Option<Instant>);

impl Deadline {
    pub fn has_passed(&self) -> bool {
        if let Some(v) = self.0 {
            Instant::now() > v
        } else {
            false
        }
    }
}

impl From<Option<Instant>> for Deadline {
    fn from(v: Option<Instant>) -> Self {
        Self(v)
    }
}

impl From<Instant> for Deadline {
    fn from(v: Instant) -> Self {
        Self(Some(v))
    }
}

impl From<Deadline> for Option<Instant> {
    fn from(v: Deadline) -> Self {
        v.0
    }
}

impl AsRef<Option<Instant>> for Deadline {
    fn as_ref(&self) -> &Option<Instant> {
        &self.0
    }
}

/// Define a timeout value for RPC.
/// This creates an Option<Duration> value which is what RPC requests expect.
#[macro_export]
macro_rules! timeout {
    (0) => {
        timeout!(None)
    };
    (0 $t:ident) => {
        timeout!(None)
    };
    (None) => {
        None
    };
    ($dur:literal s) => {
        timeout!($dur seconds)
    };
    ($dur:literal sec) => {
        timeout!($dur seconds)
    };
    ($dur:literal seconds) => {
        Some(::core::time::Duration::from_secs($dur))
    };
    ($dur:literal m) => {
        timeout!($dur minutes)
    };
    ($dur:literal min) => {
        timeout!($dur minutes)
    };
    ($dur:literal minutes) => {
        Some(::core::time::Duration::from_secs($dur * 60))
    };
    ($dur:literal h) => {
        timeout!($dur hours)
    };
    ($dur:literal hours) => {
        Some(::core::time::Duration::from_secs($dur * 60 * 60))
    };
}
