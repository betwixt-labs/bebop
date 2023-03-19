/// Assert that a type or value implements a given list of traits at compile time.
#[macro_export(local_inner_macros)]
macro_rules! const_assert_impl {
    ($val:ident: $tr:path) => {{
        const fn value_must_impl<T: $tr>(_: &T) {}
        value_must_impl(&$val);
    }};
    ($val:ident: $tr:path, $($rest:path),+) => {
        const_assert_impl!($val: $tr);
        const_assert_impl!($val: $($rest),+);
    };
    ($ty:ty: $tr:path) => {{
        const fn type_must_impl<T: $tr>() {}
        type_must_impl::<$ty>();
    }};
    ($ty:ty: $tr:path, $($rest:path),+) => {
        const_assert_impl!($ty: $tr);
        const_assert_impl!($ty: $($rest),+);
    };
    ({$val:expr}: $($tr:path),+) => {{
        let tmp = $val;
        const_assert_impl!(tmp: $($tr),+)
    }};
}

/// Ensure the alignment of a type is a certain value at compile time.
#[macro_export(local_inner_macros)]
macro_rules! const_assert_align {
    (?const_expr ($x:expr)) => {
        const _: [(); 0 - !{ const ASSERT: bool = $x; ASSERT } as usize] = [];
    };
    ($ty:ty, $align:expr) => {{
        const_assert_align!(?const_expr (::std::mem::align_of::<$ty>() == $align));
    }};
}

/// The type must be `Copy`. Creates an aligned copy of a field on the stack for easy reading.
#[macro_export(local_inner_macros)]
macro_rules! packed_read {
    ($val:ident.$field:ident) => {{
        const_assert_impl!({$val.$field}: ::std::marker::Copy);

        let brw = ::std::ptr::addr_of!($val.$field);
        unsafe { brw.read_unaligned() }
    }};
    (($expr:expr).$field:ident) => {{
        let tmp = $expr;
        packed_read!(tmp.$field)
    }};
}

/// The type must be `Copy`. Creates an aligned copy of a field on the stack for easy reading.
#[macro_export(local_inner_macros)]
macro_rules! unaligned_read {
    ($val:ident) => {{
        let brw = ::std::ptr::addr_of!(*$val);
        let alg = unsafe { brw.read_unaligned() };
        const_assert_impl!(alg: ::std::marker::Copy);
        alg
    }};
    ($expr:expr) => {{
        let tmp = $expr;
        unaligned_read!(tmp)
    }};
}

/// Reads a possibly unaligned pointer as a reference.
#[macro_export]
macro_rules! unaligned_ref_read {
    ($ptr:ident as $ty:ty) => {{
        debug_assert!(!$ptr.is_null());
        let r: $ty = unsafe { $ptr.read_unaligned() };
        r
    }};
}

/// Reads a possibly unaligned pointer as a value and passes it to a closure.
#[macro_export]
macro_rules! unaligned_do {
    ($ptr:ident as $ty:ty => |$arg:ident| $closure:expr) => {{
        debug_assert!(!$ptr.is_null());
        let $arg: $ty = unsafe { $ptr.read_unaligned() };
        let _returned = $closure;
        // we must FORGET this because `read_unaligned` makes a shallow copy.
        ::std::mem::forget($arg);
        _returned
    }};
}

/// Serialize values from a packed type.
/// Use `fn _serialize_chained` to generate a chain serialization function for use within
/// `SubRecord`, or just pass the type, value, and field to chain serialize a given value.
#[macro_export(local_inner_macros)]
macro_rules! define_serialize_chained {
    // It is a fat pointer type (e.g. &str or &[u8])
    (&$ty:ty => |$zelf:ident, $dest:ident| $closure:expr) => {
        #[inline]
        fn _serialize_chained<W: Write>(&self, dest: &mut W) -> $crate::SeResult<usize> {
            let $zelf: &$ty = self;
            let $dest = dest;
            $closure
        }

        #[inline]
        unsafe fn _serialize_chained_unaligned<W: ::std::io::Write>(zelf: *const Self, dest: &mut W) -> $crate::SeResult<usize> {
            let $zelf: &$ty = unaligned_ref_read!(zelf as &$ty);
            let $dest = dest;
            $closure
        }
    };
    // It is a copy type
    (*$ty:ty => |$zelf:ident, $dest:ident| $closure:expr) => {
        #[inline]
        fn _serialize_chained<W: ::std::io::Write>(&self, dest: &mut W) -> $crate::SeResult<usize> {
            let $zelf: $ty = *self;
            let $dest = dest;
            $closure
        }

        #[inline]
        unsafe fn _serialize_chained_unaligned<W: ::std::io::Write>(zelf: *const Self, dest: &mut W) -> $crate::SeResult<usize> {
            let $zelf: $ty = unaligned_read!(zelf);
            let $dest = dest;
            $closure
        }
    };
    // It is not a fat pointer and it is not Copy
    ($ty:ty => |$zelf:ident, $dest:ident| $closure:expr) => {
        #[inline]
        fn _serialize_chained<W: ::std::io::Write>(&self, dest: &mut W) -> $crate::SeResult<usize> {
            let $zelf: &$ty = self;
            let $dest = dest;
            $closure
        }

        #[inline]
        unsafe fn _serialize_chained_unaligned<W: ::std::io::Write>(zelf: *const Self, dest: &mut W) -> $crate::SeResult<usize> {
            let $dest = dest;
            unaligned_do!(zelf as $ty => |$zelf| $closure)
        }
    };
}
