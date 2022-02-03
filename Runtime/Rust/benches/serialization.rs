use bebop::{SliceWrapper, SubRecord, LEN_SIZE};
use criterion::{black_box, criterion_group, Criterion};
use std::collections::HashMap;
use std::io::{self, Write};

/// Not interested in testing the actual memory speed of writing since we don't have control over it
struct DummyWriter;
impl Write for DummyWriter {
    #[inline(always)]
    fn write(&mut self, buf: &[u8]) -> io::Result<usize> {
        Ok(buf.len())
    }

    #[inline(always)]
    fn flush(&mut self) -> io::Result<()> {
        Ok(())
    }
}

fn strings(c: &mut Criterion) {
    let mut buf = DummyWriter;
    c.bench_function("se empty str", |b| {
        b.iter(|| SubRecord::_serialize_chained(&black_box(""), black_box(&mut buf)).unwrap())
    });
    c.bench_function("se small str", |b| {
        b.iter(|| {
            SubRecord::_serialize_chained(&black_box(SMALL_STR), black_box(&mut buf)).unwrap()
        })
    });
    c.bench_function("se large str", |b| {
        b.iter(|| SubRecord::_serialize_chained(&black_box(LONG_STR), black_box(&mut buf)).unwrap())
    });

    let buf = [0, 0, 0, 0];
    c.bench_function("de empty str", |b| {
        b.iter(|| {
            let res = <&str>::_deserialize_chained(black_box(&buf)).unwrap();
            assert_eq!(res.1.len(), 0);
        })
    });

    let mut buf = Vec::with_capacity(SMALL_STR.len() + LEN_SIZE);
    SMALL_STR._serialize_chained(&mut buf).unwrap();
    c.bench_function("de small str", |b| {
        b.iter(|| {
            let res = <&str>::_deserialize_chained(black_box(&buf)).unwrap();
            assert_eq!(res.1.len(), SMALL_STR.len());
        })
    });

    let mut buf = Vec::with_capacity(LONG_STR.len() + LEN_SIZE);
    LONG_STR._serialize_chained(&mut buf).unwrap();
    c.bench_function("de large str", |b| {
        b.iter(|| {
            let res = <&str>::_deserialize_chained(black_box(&buf)).unwrap();
            assert_eq!(res.1.len(), LONG_STR.len());
        })
    });
}

fn vecs(c: &mut Criterion) {
    let mut buf = DummyWriter;
    let vec: Vec<i64> = (0..256).collect();
    c.bench_function("se i64 vec", |b| {
        b.iter(|| SubRecord::_serialize_chained(black_box(&vec), black_box(&mut buf)).unwrap())
    });
    let mut buf2 = Vec::with_capacity(vec.len() * 8 + LEN_SIZE);
    vec._serialize_chained(&mut buf2).unwrap();
    c.bench_function("de i64 vec", |b| {
        b.iter(|| {
            let res = <Vec<i64>>::_deserialize_chained(black_box(&buf2)).unwrap();
            assert_eq!(res.1.len(), vec.len());
        })
    });

    let vec: Vec<Vec<Vec<i64>>> = (0..4)
        .map(|_| (0..4).map(|_| (0..16).collect()).collect())
        .collect();
    c.bench_function("se i64 layered vec", |b| {
        b.iter(|| SubRecord::_serialize_chained(black_box(&vec), black_box(&mut buf)).unwrap())
    });
    let len = 8 * 4 * 4 * 16 + LEN_SIZE + LEN_SIZE * 4 + LEN_SIZE * 4 * 4;
    let mut buf2 = Vec::with_capacity(len);
    vec._serialize_chained(&mut buf2).unwrap();
    c.bench_function("de layered vec", |b| {
        b.iter(|| {
            let res = <Vec<Vec<Vec<i64>>>>::_deserialize_chained(black_box(&buf2)).unwrap();
            assert_eq!(res.0, len);
        })
    });

    let vec: Vec<&str> = LONG_STR.lines().collect();
    c.bench_function("se str vec", |b| {
        b.iter(|| SubRecord::_serialize_chained(black_box(&vec), black_box(&mut buf)).unwrap())
    });
    let mut buf2 = Vec::new();
    vec._serialize_chained(&mut buf2).unwrap();
    c.bench_function("de str vec", |b| {
        b.iter(|| {
            let res = <Vec<&str>>::_deserialize_chained(black_box(&buf2)).unwrap();
            assert_eq!(res.0, buf2.len());
        })
    });
}

fn maps(c: &mut Criterion) {
    let mut buf = DummyWriter;
    let map: HashMap<i64, i64> = (0..256).zip((0..256).rev()).collect();
    c.bench_function("se i64 map", |b| {
        b.iter(|| SubRecord::_serialize_chained(black_box(&map), black_box(&mut buf)).unwrap())
    });
    let len = 256 * 2 * 8 + LEN_SIZE;
    let mut buf2 = Vec::with_capacity(len);
    map._serialize_chained(&mut buf2).unwrap();
    c.bench_function("de i64 map", |b| {
        b.iter(|| {
            let res = <HashMap<i64, i64>>::_deserialize_chained(black_box(&buf2)).unwrap();
            assert_eq!(res.1.len(), map.len());
        })
    });

    let map: HashMap<&str, &str> = LONG_STR.lines().zip(LONG_STR.lines().rev()).collect();
    c.bench_function("se str map", |b| {
        b.iter(|| SubRecord::_serialize_chained(black_box(&map), black_box(&mut buf)).unwrap())
    });
    let mut buf2 = Vec::with_capacity(len);
    map._serialize_chained(&mut buf2).unwrap();
    c.bench_function("de str map", |b| {
        b.iter(|| {
            let res = <HashMap<&str, &str>>::_deserialize_chained(black_box(&buf2)).unwrap();
            assert_eq!(res.0, buf2.len());
        })
    });
}

fn slice_wrappers(c: &mut Criterion) {
    let mut buf = DummyWriter;
    let vec: Vec<i64> = (0..256).collect();
    let sw = SliceWrapper::Cooked(&vec);
    c.bench_function("se i64 cooked slicewrapper", |b| {
        b.iter(|| {
            SubRecord::_serialize_chained(black_box(&sw), black_box(&mut buf)).unwrap();
        })
    });
    let mut buf2 = Vec::with_capacity(sw.len() * 8 + LEN_SIZE);
    sw._serialize_chained(&mut buf2).unwrap();
    c.bench_function("de i64 cooked slicewrapper", |b| {
        b.iter(|| {
            let res = <SliceWrapper<i64>>::_deserialize_chained(black_box(&buf2)).unwrap();
            assert_eq!(res.1.len(), sw.len());
        })
    });
}

const SMALL_STR: &str = "Hello World!";
const LONG_STR: &str = r#"
Lorem ipsum dolor sit amet, consectetur adipiscing elit. Vivamus non consectetur massa.
Nulla ultricies id massa vitae lacinia. Sed posuere, leo sit amet condimentum sodales, mi purus
semper metus, id imperdiet mi erat vel turpis. Proin dictum ornare lacus, nec fermentum nibh
fermentum quis. Nulla tristique facilisis neque eget facilisis. Ut ut lacinia massa, quis vulputate
diam. Integer eleifend nulla metus, a venenatis ex feugiat nec.

Aliquam ac ornare nulla. Duis et diam dui. Donec at dui vehicula, mattis tortor quis, sagittis leo.
In quis nisi ac odio scelerisque consequat. Curabitur quis egestas orci. Nullam convallis urna nisl,
ac pharetra ante rutrum id. Ut magna metus, rutrum tempus lorem eget, sollicitudin cursus eros.

Morbi accumsan purus a dui placerat, sed sollicitudin purus finibus. Mauris luctus purus ultrices
finibus egestas. Donec vulputate vestibulum metus sed convallis. Nulla pharetra, tellus at luctus
tristique, ante ex sodales urna, a congue enim urna feugiat ante. Sed accumsan sed urna sed
tristique. Etiam at lectus nec nisi cursus mattis. Integer vulputate vehicula nibh, sit amet
fringilla diam fermentum sit amet. Morbi vitae euismod erat, vel sollicitudin lacus. Proin et
venenatis diam, vitae volutpat tortor. Cras mattis luctus leo, vel tincidunt nisl scelerisque et.

Maecenas gravida blandit neque id placerat. Fusce ac enim nisl. Duis lobortis arcu a tortor volutpat
fermentum. Nam id sapien tristique, posuere ipsum vel, venenatis neque. Sed nec dictum nisi, ut
hendrerit ipsum. Sed faucibus congue sodales. Sed eu aliquam lorem, nec suscipit elit. Morbi
lobortis, erat at dapibus vestibulum, felis lectus facilisis magna, ut ullamcorper risus diam eget
lectus. Phasellus quis velit neque. Phasellus leo diam, pulvinar id odio vitae, dapibus tincidunt
nibh. Nam justo velit, imperdiet sed pellentesque in, scelerisque vel mauris. Proin tellus ligula,
interdum a tempus vel, euismod a nisi. Suspendisse egestas quis velit vitae fermentum. Pellentesque
ut nisl quam. Nullam egestas, ante sit amet pharetra laoreet, purus mauris sollicitudin magna, in
sagittis eros sem eu elit.

Maecenas vehicula lectus sed purus ultricies fermentum. Pellentesque habitant morbi tristique
senectus et netus et malesuada fames ac turpis egestas. Donec aliquet venenatis tellus, sed pretium
sapien molestie quis. Donec egestas eros non finibus ultrices. Maecenas cursus diam id dui euismod
feugiat. Sed mauris nisl, rutrum sit amet cursus non, facilisis et mauris. Proin dictum porta nisi,
quis auctor velit egestas sed. Sed efficitur dolor nec gravida pharetra. Nullam dignissim pretium
risus, ac congue neque blandit vel. Maecenas sit amet turpis ac sapien accumsan dignissim at in
quam. Maecenas mollis interdum scelerisque. Fusce eget lorem eu lacus feugiat condimentum vitae ut
nibh.
"#;

criterion_group!(benches, strings, vecs, maps, slice_wrappers);
