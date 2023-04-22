use crate::generated::jazz::*;
use bebop::{test_serialization, collection, Date, Guid, Record, SubRecord, ENUM_SIZE, LEN_SIZE};

fn song1() -> Song<'static> {
    Song {
        title: Some("A Night in Tunisia"),
        year: Some(1942),
        performers: Some(vec![
            Performer {
                name: "Dizzy Gillespie",
                plays: Instrument::Trumpet,
            },
            Performer {
                name: "Frank Paparelli",
                plays: Instrument::Piano,
            },
        ]),
    }
}

fn song2() -> Song<'static> {
    Song {
        title: Some("Ornithology"),
        year: Some(1946),
        performers: None,
    }
}

#[test]
fn product_id_defined() {
    assert_eq!(
        IMPORTANT_PRODUCT_ID,
        Guid::from_be_bytes([
            0xa3, 0x62, 0x8e, 0xc7, 0x28, 0xd4, 0x45, 0x46, 0xad, 0x4a, 0xf6, 0xeb, 0xf5, 0x37,
            0x5c, 0x96,
        ])
    );
}

#[test]
fn product_id_defined_owned() {
    assert_eq!(
        owned::IMPORTANT_PRODUCT_ID,
        Guid::from_be_bytes([
            0xa3, 0x62, 0x8e, 0xc7, 0x28, 0xd4, 0x45, 0x46, 0xad, 0x4a, 0xf6, 0xeb, 0xf5, 0x37,
            0x5c, 0x96,
        ])
    );
}

#[test]
fn piano_keys_defined() {
    assert_eq!(PIANO_KEYS, 88i32);
}

#[test]
fn instrument_values_correct() {
    assert_eq!(u32::from(Instrument::Sax), 0);
    assert_eq!(u32::from(Instrument::Trumpet), 1);
    assert_eq!(u32::from(Instrument::Clarinet), 2);
    assert_eq!(u32::from(Instrument::Piano), 5);
}

#[test]
fn instrument_values_correct_when_serialized() {
    let mut buf = Vec::new();
    Instrument::Sax._serialize_chained(&mut buf).unwrap();
    assert_eq!(buf[0..ENUM_SIZE], [0x00, 0x00, 0x00, 0x00]);
    buf.clear();
    Instrument::Trumpet._serialize_chained(&mut buf).unwrap();
    assert_eq!(buf[0..ENUM_SIZE], [0x01, 0x00, 0x00, 0x00]);
    buf.clear();
    Instrument::Clarinet._serialize_chained(&mut buf).unwrap();
    assert_eq!(buf[0..ENUM_SIZE], [0x02, 0x00, 0x00, 0x00]);
    buf.clear();
    Instrument::Piano._serialize_chained(&mut buf).unwrap();
    assert_eq!(buf[0..ENUM_SIZE], [0x05, 0x00, 0x00, 0x00]);
    buf.clear();
}

#[test]
fn validate_memory_logic() {
    assert_eq!(std::mem::size_of::<Instrument>(), ENUM_SIZE);
}

#[test]
fn borrowed_and_owned_serialized_size_predictions_match() {
    let b = song1();
    let o: owned::Song = b.clone().into();
    assert_eq!(b.serialized_size(), o.serialized_size());

    let b = song2();
    let o: owned::Song = b.clone().into();
    assert_eq!(b.serialized_size(), o.serialized_size());
}

#[test]
fn borrowed_and_owned_serialized_forms_match() {
    for b in &[song1(), song2()] {
        let mut buf1 = Vec::new();
        let mut buf2 = Vec::new();
        let o: owned::Song = b.clone().into();
        b.serialize(&mut buf1).unwrap();
        o.serialize(&mut buf2).unwrap();
        assert_eq!(buf1, buf2);
    }
}

test_serialization!(
    serialization_of_instrument,
    Instrument,
    Instrument::Trumpet,
    ENUM_SIZE
);
test_serialization!(
    serialization_of_instrument_owned,
    owned::Instrument,
    owned::Instrument::Trumpet,
    ENUM_SIZE
);
test_serialization!(
    serialization_of_performer,
    Performer,
    Performer {
        name: "Charlie Parker",
        plays: Instrument::Sax,
    },
    LEN_SIZE + 14 + ENUM_SIZE
);
test_serialization!(
    serialization_of_performer_owned,
    owned::Performer,
    Performer {
        name: "Charlie Parker",
        plays: Instrument::Sax,
    }.into(),
    LEN_SIZE + 14 + ENUM_SIZE
);
test_serialization!(
    serialization_of_song_all_fields,
    Song,
    song1(),
    LEN_SIZE * 5 + 4 + 18 + 2 + 15 + 15 + ENUM_SIZE * 2
);
test_serialization!(
    serialization_of_song_all_fields_owned,
    owned::Song,
    song1().into(),
    LEN_SIZE * 5 + 4 + 18 + 2 + 15 + 15 + ENUM_SIZE * 2
);
test_serialization!(
    serialization_of_song_some_fields,
    Song,
    song2(),
    LEN_SIZE * 2 + 3 + 11 + 2
);
test_serialization!(
    serialization_of_song_some_fields_owned,
    owned::Song,
    song2().into(),
    LEN_SIZE * 2 + 3 + 11 + 2
);
test_serialization!(
    serialization_of_song_no_fields,
    Song,
    Song::default(),
    5
);
test_serialization!(
    serialization_of_song_no_fields_owned,
    owned::Song,
    Song::default().into(),
    5
);
test_serialization!(
    serialization_of_studio_album,
    Album,
    Album::StudioAlbum {
        tracks: vec![song1(), song2()],
    },
    115
);
test_serialization!(
    serialization_of_studio_album_owned,
    owned::Album,
    Album::StudioAlbum {
        tracks: vec![song1(), song2()],
    }.into(),
    115
);
test_serialization!(
    serialization_of_studio_album_empty_array,
    Album,
    Album::StudioAlbum {
        tracks: vec![]
    },
    LEN_SIZE * 2 + 1
);
test_serialization!(
    serialization_of_studio_album_empty_array_owned,
    owned::Album,
    Album::StudioAlbum {
        tracks: vec![]
    }.into(),
    LEN_SIZE * 2 + 1
);
test_serialization!(
    serialization_of_live_album_all_fields,
    Album,
    Album::LiveAlbum {
        tracks: Some(vec![song1(), song2()]),
        venue_name: Some("Perdido"),
        concert_date: Some(Date::from_secs(1627595855)),
    },
    142
);
test_serialization!(
    serialization_of_live_album_all_fields_owned,
    owned::Album,
    Album::LiveAlbum {
        tracks: Some(vec![song1(), song2()]),
        venue_name: Some("Perdido"),
        concert_date: Some(Date::from_secs(1627595855)),
    }.into(),
    142
);
test_serialization!(
    serialization_of_live_album_some_fields,
    Album,
    Album::LiveAlbum {
        tracks: None,
        venue_name: Some("Perdido"),
        concert_date: Some(Date::from_secs(1627595855)),
    },
    31
);
test_serialization!(
    serialization_of_live_album_some_fields_owned,
    owned::Album,
    Album::LiveAlbum {
        tracks: None,
        venue_name: Some("Perdido"),
        concert_date: Some(Date::from_secs(1627595855)),
    }.into(),
    31
);
test_serialization!(
    serialization_of_live_album_no_fields,
    Album,
    Album::LiveAlbum {
        tracks: None,
        venue_name: None,
        concert_date: None,
    },
    10
);
test_serialization!(
    serialization_of_live_album_no_fields_owned,
    owned::Album,
    Album::LiveAlbum {
        tracks: None,
        venue_name: None,
        concert_date: None,
    }.into(),
    10
);
test_serialization!(
    serialization_of_empty_library,
    Library,
    Library {
        albums: collection! {}
    },
    LEN_SIZE
);
test_serialization!(
    serialization_of_empty_library_owned,
    owned::Library,
    Library {
        albums: collection! {}
    }.into(),
    LEN_SIZE
);
test_serialization!(
    serialization_of_library_studio_album_empty,
    Library,
    Library {
        albums: collection! {
            "Milestones" => Album::StudioAlbum {
                tracks: vec![]
            }
        }
    },
    // map, vec, string, and union have size
    LEN_SIZE * 4 + 10 + 1
);
test_serialization!(
    serialization_of_library_studio_album_empty_owned,
    owned::Library,
    Library {
        albums: collection! {
            "Milestones" => Album::StudioAlbum {
                tracks: vec![]
            }
        }
    }.into(),
    // map, vec, string, and union have size
    LEN_SIZE * 4 + 10 + 1
);

#[test]
fn deserialization_of_song_unknown_fields() {
    let buf = [
        0x07, 0x00, 0x00, 0x00, 0x02, 0xe5, 0x07, 0x08, 0xff, 0xff, 0x00,
    ];
    let de_song = Song::deserialize(&buf).unwrap();
    assert_eq!(
        de_song,
        Song {
            title: None,
            year: Some(2021),
            performers: None
        }
    );
}

#[test]
fn deserialization_of_unknown_album() {
    let buf = [0x04, 0x00, 0x00, 0x00, 0x05, 0x00, 0x00, 0x00, 0x00];
    let (read, de_album) = Album::_deserialize_chained(&buf).unwrap();
    assert_eq!(read, 9);
    assert_eq!(de_album, Album::Unknown);
}
