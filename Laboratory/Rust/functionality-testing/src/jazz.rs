use crate::generated::jazz::*;
use bebop::{Date, Guid, Record, SubRecord, ENUM_SIZE, LEN_SIZE};

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
fn serialization_of_instrument() {
    let mut buf = Vec::new();
    assert_eq!(
        Instrument::Trumpet._serialize_chained(&mut buf).unwrap(),
        ENUM_SIZE
    );
    assert_eq!(buf.len(), ENUM_SIZE);
    assert_eq!(buf[0..ENUM_SIZE], [0x01, 0x00, 0x00, 0x00]);
    let (read, instrument) = Instrument::_deserialize_chained(&buf).unwrap();
    assert_eq!(read, ENUM_SIZE);
    assert_eq!(instrument, Instrument::Trumpet);
}

#[test]
fn serialization_of_performer() {
    let mut buf = Vec::new();
    let perf = Performer {
        name: "Charlie Parker",
        plays: Instrument::Sax,
    };
    const LEN: usize = LEN_SIZE + 14 + ENUM_SIZE;
    assert_eq!(perf.serialize(&mut buf).unwrap(), LEN);
    assert_eq!(buf.len(), LEN);

    let de_perf = Performer::deserialize(&buf).unwrap();
    assert_eq!(de_perf, perf);
}

#[test]
fn serialization_of_song_all_fields() {
    let mut buf = Vec::new();
    let song = song1();
    song.serialize(&mut buf).unwrap();
    assert_eq!(
        buf.len(),
        LEN_SIZE * 5 + 4 + 18 + 2 + 15 + 15 + ENUM_SIZE * 2
    );
    let de_song = Song::deserialize(&buf).unwrap();
    assert_eq!(de_song, song);
}

#[test]
fn serialization_of_song_some_fields() {
    let mut buf = Vec::new();
    let song = song2();
    song.serialize(&mut buf).unwrap();
    assert_eq!(buf.len(), LEN_SIZE * 2 + 3 + 11 + 2);
    let de_song = Song::deserialize(&buf).unwrap();
    assert_eq!(de_song, song);
}

#[test]
fn serialization_of_song_no_fields() {
    let mut buf = Vec::new();
    let song = Song::default();
    song.serialize(&mut buf).unwrap();
    assert_eq!(buf.len(), 5);
    assert_eq!(buf[0..5], [0x01, 0x00, 0x00, 0x00, 0x00]);
    let de_song = Song::deserialize(&buf).unwrap();
    assert_eq!(de_song, song);
}

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
fn serialization_of_studio_album() {
    let mut buf = Vec::new();
    let album = Album::StudioAlbum {
        tracks: vec![song1(), song2()],
    };
    album.serialize(&mut buf).unwrap();
    let de_album = Album::deserialize(&buf).unwrap();
    assert_eq!(album, de_album);
}

#[test]
fn serialization_of_live_album_all_fields() {
    let mut buf = Vec::new();
    let album = Album::LiveAlbum {
        tracks: Some(vec![song1(), song2()]),
        venue_name: Some("Perdido"),
        concert_date: Some(Date::from_secs(1627595855)),
    };
    album.serialize(&mut buf).unwrap();
    let de_album = Album::deserialize(&buf).unwrap();
    // println!("{}", de_album.unwrap_err());
    assert_eq!(album, de_album);
}

#[test]
fn serialization_of_live_album_some_fields() {
    let mut buf = Vec::new();
    let album = Album::LiveAlbum {
        tracks: None,
        venue_name: Some("Perdido"),
        concert_date: Some(Date::from_secs(1627595855)),
    };
    album.serialize(&mut buf).unwrap();
    let de_album = Album::deserialize(&buf).unwrap();
    assert_eq!(album, de_album);
}

#[test]
fn serialization_of_live_album_no_fields() {
    let mut buf = Vec::new();
    let album = Album::LiveAlbum {
        tracks: None,
        venue_name: None,
        concert_date: None,
    };
    album.serialize(&mut buf).unwrap();
    let de_album = Album::deserialize(&buf).unwrap();
    assert_eq!(album, de_album);
}

#[test]
fn deserialization_of_unknown_album() {
    let buf = [0x04, 0x00, 0x00, 0x00, 0x05, 0x00, 0x00, 0x00, 0x00];
    let (read, de_album) = Album::_deserialize_chained(&buf).unwrap();
    assert_eq!(read, 9);
    assert_eq!(de_album, Album::Unknown);
}
