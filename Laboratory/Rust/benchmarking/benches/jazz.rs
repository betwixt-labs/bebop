use bebop::{collection, Record};
use benchmarking::*;
use criterion::{black_box, criterion_group, Criterion};
use protobuf::{Message, ProtobufEnum};

use bebops::jazz as bb;
use native::jazz as na;
use protos::jazz as pr;
use serde::Serialize;

fn make_library() -> native::jazz::Library {
    // to whomever is reading this, yes, I was very lazy in pulling details from wikipedia.
    na::Library {
        songs: collection! {
            "A Night in Tunisia".into() => na::Song {
                title: Some("A Night in Tunisia".into()),
                year: Some(1942),
                performers: Some(vec![
                    na::Performer {
                        name: "Dizzy Gillespie".into(),
                        plays: na::Instrument::Trumpet
                    },
                    na::Performer {
                        name: "Frank Paparelli".into(),
                        plays: na::Instrument::Piano
                    },
                    na::Performer {
                        name: "Count Basie".into(),
                        plays: na::Instrument::Piano
                    }
                ])
            },
            "'Round Midnight".into() => na::Song {
                title: Some("'Round Midnight".into()),
                year: Some(1986),
                performers: Some(vec![
                    na::Performer {
                        name: "Freddie Hubbard".into(),
                        plays: na::Instrument::Trumpet
                    },
                    na::Performer {
                        name: "Ron Carter".into(),
                        plays: na::Instrument::Cello
                    },
                ])
            },
            "Bouncing with Bud".into() => na::Song {
                title: Some("Bounding with Bud".into()),
                year: Some(1946),
                performers: None
            },
            "Groovin' High".into() => na::Song {
                title: None,
                year: None,
                performers: None
            },
            "Song for My Father".into() => na::Song {
                title: Some("Song for My Father".into()),
                year: Some(1965),
                performers: Some(vec![
                    na::Performer {
                        name: "Horace Silver".into(),
                        plays: na::Instrument::Piano
                    },
                    na::Performer {
                        name: "Carmell Jones".into(),
                        plays: na::Instrument::Trumpet
                    },
                    na::Performer {
                        name: "Joe Henderson".into(),
                        plays: na::Instrument::Sax
                    },
                    na::Performer {
                        name: "Teddy Smith".into(),
                        plays: na::Instrument::Cello
                    }
                ])
            },
        },
    }
}
const ALLOC_SIZE: usize = 1024 * 1024 * 4;

fn library(c: &mut Criterion) {
    // structuring
    let mut structuring_group = c.benchmark_group("Jazz Library Structuring");
    structuring_group.bench_function("Native", |b| {
        b.iter(|| {
            make_library();
        })
    });

    // don't penalize the others for the initial allocation
    let native_struct = make_library();
    structuring_group.bench_function("Bebop", |b| {
        b.iter(|| {
            bb::Library::from(black_box(&native_struct));
        })
    });
    structuring_group.bench_function("Protobuf", |b| {
        b.iter(|| {
            // well protobuf requires cloning, so we might need a test with the cloning extracted, but
            // I also suspect it is quite common in real world to need to clone the data.
            // TODO: Figure out how to test without cloning in the performance test?
            pr::Library::from(black_box(native_struct.clone()));
        })
    });
    structuring_group.finish();

    // serialization
    let bebop_struct = bb::Library::from(&native_struct);
    let proto_struct = pr::Library::from(native_struct.clone());
    let mut buf = Vec::with_capacity(ALLOC_SIZE);
    let mut serialization_group = c.benchmark_group("Jazz Library Serialization");

    // serialization_group.bench_function("Json", |b| {
    //     b.iter(|| {
    //         serde_json::to_writer(&mut buf, &native_struct).unwrap();
    //         assert!(buf.len() <= ALLOC_SIZE);
    //         buf.clear();
    //     })
    // });

    // serialization_group.bench_function("Bincode", |b| {
    //     b.iter(|| {
    //         bincode::serialize_into(&mut buf, &native_struct).unwrap();
    //         assert!(buf.len() <= ALLOC_SIZE);
    //         buf.clear();
    //     })
    // });

    serialization_group.bench_function("MessagePack", |b| {
        b.iter(|| {
            rmp_serde::encode::write(&mut buf, &native_struct).unwrap();
            assert!(buf.len() <= ALLOC_SIZE);
            buf.clear();
        })
    });

    serialization_group.bench_function("Bebop", |b| {
        b.iter(|| {
            bebop_struct.serialize(&mut buf).unwrap();
            assert!(buf.len() <= ALLOC_SIZE);
            buf.clear();
        })
    });

    serialization_group.bench_function("Protobuf", |b| {
        b.iter(|| {
            proto_struct.write_to_writer(&mut buf).unwrap();
            assert!(buf.len() <= ALLOC_SIZE);
            buf.clear();
        })
    });

    serialization_group.finish();

    let mut deserialization_group = c.benchmark_group("Jazz Library Deserialization");
    // serde_json::to_writer(&mut buf, &native_struct).unwrap();
    // deserialization_group.bench_function("Json", |b| {
    //     b.iter(|| {
    //         serde_json::from_reader::<_, na::Library>(black_box(&*buf)).unwrap();
    //     })
    // });
    // buf.clear();

    // bincode::serialize_into(&mut buf, &native_struct).unwrap();
    // deserialization_group.bench_function("Bincode", |b| {
    //     b.iter(|| {
    //         bincode::deserialize_from::<_, na::Library>(black_box(&*buf)).unwrap();
    //     })
    // });
    // buf.clear();

    rmp_serde::encode::write(&mut buf, &native_struct).unwrap();
    deserialization_group.bench_function("MessagePack", |b| {
        b.iter(|| {
            rmp_serde::from_read::<_, na::Library>(black_box(&*buf)).unwrap();
        })
    });
    buf.clear();

    bebop_struct.serialize(&mut buf).unwrap();
    deserialization_group.bench_function("Bebop", |b| {
        b.iter(|| {
            bb::Library::deserialize(black_box(&buf)).unwrap();
        })
    });
    buf.clear();

    proto_struct.write_to_writer(&mut buf).unwrap();
    deserialization_group.bench_function("Protobuf", |b| {
        b.iter(|| {
            pr::Library::parse_from_bytes(&buf).unwrap();
        })
    });
}

fn album(c: &mut Criterion) {}

criterion_group!(benches, library, album);
