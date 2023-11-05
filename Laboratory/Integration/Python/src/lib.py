from schema import Library, Instrument, Album, StudioAlbum, LiveAlbum, Song, Musician
from datetime import datetime

def make_lib():
    giant_steps_song = Song()
    giant_steps_song.title = "Giant Steps"
    giant_steps_song.year = 1959
    giant_steps_song.performers = [
        Musician(name="John Coltrane", plays=Instrument.PIANO)
    ]

    a_night_in_tunisia_song = Song()
    a_night_in_tunisia_song.title = "A Night in Tunisia"
    a_night_in_tunisia_song.year = 1942
    a_night_in_tunisia_song.performers = [
        Musician(name="Dizzy Gillespie", plays=Instrument.TRUMPET),
        Musician(name="Count Basie", plays=Instrument.PIANO),
    ]

    groovin_high_song = Song()
    groovin_high_song.title = "Groovin' High"

    adams_apple_album = LiveAlbum()
    adams_apple_album.venueName = "Tunisia"
    adams_apple_album.concertDate = datetime.fromtimestamp(528205479)

    unnamed_song = Song()
    unnamed_song.year = 1965
    unnamed_song.performers = [
        Musician(name="Carmell Jones", plays=Instrument.TRUMPET),
        Musician(name="Joe Henderson", plays=Instrument.SAX),
        Musician(name="Teddy Smith", plays=Instrument.CLARINET)
    ]

    brilliant_corners_album = LiveAlbum()
    brilliant_corners_album.venueName = "Night's Palace"
    brilliant_corners_album.tracks = [
        unnamed_song
    ]

    return Library(albums={
        "Giant Steps": Album.fromStudioAlbum(StudioAlbum(tracks=[
            giant_steps_song, a_night_in_tunisia_song, groovin_high_song
        ])),
        "Adam's Apple": Album.fromLiveAlbum(adams_apple_album),
        "Milestones": Album.fromStudioAlbum(StudioAlbum(tracks=[])),
        "Brilliant Corners": Album.fromLiveAlbum(brilliant_corners_album)
    })