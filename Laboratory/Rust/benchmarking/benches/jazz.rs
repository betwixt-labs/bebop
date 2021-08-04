use bebop::collection;
use benchmarking::*;
use criterion::{criterion_group, Criterion};
use protobuf::ProtobufEnum;

// to whomever is reading this, yes, I was very lazy in pulling details from wikipedia.

fn bebop_library() -> bebops::jazz::Library<'static> {
    bebops::jazz::Library {
        songs: collection! {
            "A Night in Tunisia" => bebops::jazz::Song {
                title: Some("A Night in Tunisia"),
                year: Some(1942),
                performers: Some(vec![
                    bebops::jazz::Performer {
                        name: "Dizzy Gillespie",
                        plays: bebops::jazz::Instrument::Trumpet
                    },
                    bebops::jazz::Performer {
                        name: "Frank Paparelli",
                        plays: bebops::jazz::Instrument::Piano
                    },
                    bebops::jazz::Performer {
                        name: "Count Basie",
                        plays: bebops::jazz::Instrument::Piano
                    }
                ])
            },
            "'Round Midnight" => bebops::jazz::Song {
                title: Some("'Round Midnight"),
                year: Some(1986),
                performers: Some(vec![
                    bebops::jazz::Performer {
                        name: "Freddie Hubbard",
                        plays: bebops::jazz::Instrument::Trumpet
                    },
                    bebops::jazz::Performer {
                        name: "Ron Carter",
                        plays: bebops::jazz::Instrument::Cello
                    },
                ])
            },
            "Bouncing with Bud" => bebops::jazz::Song {
                title: Some("Bounding with Bud"),
                year: Some(1946),
                performers: None
            },
            "Groovin' High" => bebops::jazz::Song::default(),
            "Song for My Father" => bebops::jazz::Song {
                title: Some("Song for My Father"),
                year: Some(1965),
                performers: Some(vec![
                    bebops::jazz::Performer {
                        name: "Horace Silver",
                        plays: bebops::jazz::Instrument::Piano
                    },
                    bebops::jazz::Performer {
                        name: "Carmell Jones",
                        plays: bebops::jazz::Instrument::Trumpet
                    },
                    bebops::jazz::Performer {
                        name: "Joe Henderson",
                        plays: bebops::jazz::Instrument::Sax
                    },
                    bebops::jazz::Performer {
                        name: "Teddy Smith",
                        plays: bebops::jazz::Instrument::Cello
                    }
                ])
            },
        },
    }
}

fn protobuf_library() -> protos::jazz::Library {
    // Wow, they could not even be bothered to support chaining... This is painful.
    // I was going to define these as literals but protobuf broke my spirit.
    let bebop_library = bebop_library();

    let mut library = protos::jazz::Library::new();
    library.set_songs(
        bebop_library
            .songs
            .into_iter()
            .map(|(name, bsong)| {
                let mut song = protos::jazz::Song::new();
                if let Some(title) = bsong.title {
                    song.set_title(title.into())
                }
                if let Some(year) = bsong.year {
                    song.set_year(year as u32)
                }
                if let Some(performers) = bsong.performers {
                    song.set_performers(
                        performers
                            .into_iter()
                            .map(|bperformer| {
                                let mut performer = protos::jazz::Performer::new();
                                performer.set_name(bperformer.name.into());
                                performer.set_plays(
                                    protos::jazz::Instrument::from_i32(
                                        u32::from(bperformer.plays) as i32
                                    )
                                    .unwrap(),
                                );
                                performer
                            })
                            .collect(),
                    )
                }
                (name.into(), song)
            })
            .collect(),
    );
    library
}

fn serde_library() -> serdes::jazz::Library {
    serdes::jazz::Library {
        songs: bebop_library()
            .songs
            .into_iter()
            .map(|(name, bsong)| {
                (
                    name.into(),
                    serdes::jazz::Song {
                        title: bsong.title.map(ToOwned::to_owned),
                        year: bsong.year,
                        performers: bsong.performers.map(|performers| {
                            performers
                                .into_iter()
                                .map(|bperformer| serdes::jazz::Performer {
                                    name: bperformer.name.into(),
                                    plays: u32::from(bperformer.plays).into(),
                                })
                                .collect()
                        }),
                    },
                )
            })
            .collect(),
    }
}

fn library(c: &mut Criterion) {
    let mut se_group = c.benchmark_group("Jazz Library Serialization");

    let bebop_lib = bebop_library();
    let protobuf_lib = protobuf_library();
    let serde_lib = serde_library();

    se_group.bench_function("Bebop", |b| b.iter(|| {}));

    se_group.bench_function("Protobuf", |b| b.iter(|| {}));

    se_group.bench_function("JSON", |b| b.iter(|| {}));
    se_group.bench_function("MessagePack", |b| b.iter(|| {}));
    se_group.bench_function("Bincode", |b| b.iter(|| {}));
    se_group.bench_function("Flexbuffers", |b| b.iter(|| {}));
    se_group.finish();
}

fn album(c: &mut Criterion) {}

criterion_group!(benches, library, album);
