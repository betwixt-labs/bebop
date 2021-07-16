//! Manual implementation which can be used for testing coherence and also for testing performance
//!
//! ```
//! const int32 PianoKeys = 88;
//! const guid ImportantProductID = "a3628ec7-28d4-4546-ad4a-f6ebf5375c96";
//!
//! enum Instrument {
//!     Sax = 0;
//!     Trumpet = 1;
//!     Clarinet = 2;
//! }
//!
//! struct Performer {
//!     string name;
//!     Instrument plays;
//! }
//!
//! message Song {
//!     1 -> string title;
//!     2 -> uint16 year;
//!     3 -> Performer[] performers;
//! }
//!
//! union Album {
//!     1 -> struct StudioAlbum {
//!         Song[] tracks;
//!     }
//!     2 -> message LiveAlbum {
//!         1 -> Song[] tracks;
//!         2 -> string venueName;
//!         3 -> date concertDate;
//!     }
//! }
//! ```

use bebop::*;

const PIANO_KEYS: i32 = 88;
const IMPORTANT_PRODUCT_ID: Guid = Guid::from_be_bytes([
    0xa3, 0x62, 0x8e, 0xc7, 0x28, 0xd4, 0x45, 0x46, 0xad, 0x4a, 0xf6, 0xeb, 0xf5, 0x37, 0x5c, 0x96,
]);
