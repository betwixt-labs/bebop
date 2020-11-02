import { ILibrary, Instrument, Library, ISong, Song, Musician } from '../test/generated/jazz';
import * as assert from "assert";
import benchmark = require('benchmark');
import { BebopView } from '../test/generated/BebopView';
const suite = new benchmark.Suite();

const song1: ISong = {
    title: 'Donna Lee',
    year: 1947,
    performers: [
        { name: 'Charlie Parker', plays: Instrument.Sax },
        { name: 'Miles Davis', plays: Instrument.Trumpet },
    ],
};

const song2: ISong = {
    title: 'A Night In Tunisia',
    year: 1946,
    performers: [
        { name: 'Dizzy Gillespie', plays: Instrument.Trumpet },
    ],
};

const library: ILibrary = {
    songs: new Map([
        ['4f40c472-2eca-4375-b9c6-f8aa87684579', song1],
        ['2dd446ef-85b4-40fd-91d1-40dbfb93d8c7', song2],
    ]),
}

const bebopEncodedSong1 = Song.encode(song1);
const jsonEncodedSong1 = JSON.stringify(song1);

/////////////////////////////////////

suite.add('Bebop-encode Song', function () {
    Song.encode(song1);
})

suite.add('JSON.stringify Song', function () {
    JSON.stringify(song1);
})

suite.add('Bebop-decode Song', function () {
    Song.decode(bebopEncodedSong1);
})

suite.add('JSON.parse Song', function () {
    JSON.parse(jsonEncodedSong1);
})

suite.on('cycle', cycle)

suite.run()

function cycle (e) {
  console.log(e.target.toString())
}
