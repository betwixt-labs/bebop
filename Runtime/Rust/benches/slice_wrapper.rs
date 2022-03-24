use bebop::error::{DeResult, SeResult};
use bebop::fixed_sized::FixedSized;
use bebop::SubRecord;
use bebop::{SliceWrapper, LEN_SIZE};
use criterion::{black_box, criterion_group, Criterion};
use std::convert::TryInto;
use std::io::Write;

/// a struct designed to be a nightmare for alignment
#[repr(packed)]
#[derive(Debug, Eq, PartialEq, Copy, Clone)]
struct Fixed {
    a: u8,
    b: u64,
}

impl FixedSized for Fixed {}

impl<'raw> SubRecord<'raw> for Fixed {
    const MIN_SERIALIZED_SIZE: usize = Self::SERIALIZED_SIZE;
    const EXACT_SERIALIZED_SIZE: Option<usize> = Some(Self::SERIALIZED_SIZE);

    fn serialized_size(&self) -> usize {
        Self::SERIALIZED_SIZE
    }

    fn _serialize_chained<W: Write>(&self, dest: &mut W) -> SeResult<usize> {
        self.a._serialize_chained(dest)?;
        self.b._serialize_chained(dest)?;
        Ok(9)
    }

    fn _deserialize_chained(raw: &'raw [u8]) -> DeResult<(usize, Self)> {
        Ok((
            9,
            Self {
                a: raw[0],
                b: u64::from_le_bytes((raw[1..9]).try_into().unwrap()),
            },
        ))
    }
}

fn cooked_array() -> Vec<Fixed> {
    (0..512u64)
        .map(|i| Fixed {
            a: (i % 255) as u8,
            b: i * 871 + i,
        })
        .collect()
}

fn getter(c: &mut Criterion) {
    let cooked = cooked_array();
    let raw = {
        let len = Fixed::SERIALIZED_SIZE * 512 + LEN_SIZE;
        let mut buf = Vec::with_capacity(len);
        assert_eq!(cooked._serialize_chained(&mut buf).unwrap(), len);
        buf
    };

    let sw = <SliceWrapper<Fixed>>::from_raw(&raw[LEN_SIZE..]);
    c.bench_function("SliceWrapper get cooked struct", |b| {
        b.iter(|| {
            for i in 0..512 {
                sw.get(black_box(i)).unwrap();
            }
        })
    });

    let sw = <SliceWrapper<Fixed>>::from_cooked(&cooked);
    c.bench_function("SliceWrapper get cooked struct", |b| {
        b.iter(|| {
            for i in 0..512 {
                sw.get(black_box(i)).unwrap();
            }
        })
    });
}

criterion_group!(benches, getter);
