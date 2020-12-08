import { ILibrary, Instrument, Library, ISong, Song, Musician } from '../test/generated/jazz';
import * as assert from "assert";
import benchmark = require('benchmark');
import msgpack = require("@msgpack/msgpack");
import Pbf = require('pbf');

// This file was generated with `pbf ./jazz.proto > ./jazz_protobuf.gen.js`,
// which is how `pbf` (the ProtoBuf implementation benchmarked here) works.
var PbfSong = require('./jazz_protobuf.gen').Song;

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

const bebopEncodedSong = Song.encode(song1);
const jsonEncodedSong = JSON.stringify(song1);
const msgpackEncodedSong = msgpack.encode(song1);
var pbf = new Pbf();
PbfSong.write(song1, pbf);
const protobufEncodedSong = pbf.finish();

/////////////////////////////////////

suite.add('Bebop   encode Song', () => Song.encode(song1));
suite.add('Bebop   decode Song', () => Song.decode(bebopEncodedSong));
suite.add('JSON    encode Song', () => JSON.stringify(song1));
suite.add('JSON    decode Song', () => JSON.parse(jsonEncodedSong));
suite.add('msgpack encode Song', () => msgpack.encode(song1));
suite.add('msgpack decode Song', () => msgpack.decode(msgpackEncodedSong));
suite.add('pbf     encode Song', () => { var p = new Pbf(); PbfSong.write(song1, p); p.finish(); });
suite.add('pbf     decode Song', () => PbfSong.read(new Pbf(protobufEncodedSong)));

suite.on('cycle', cycle)

suite.run()

function cycle (e) {
  console.log(e.target.toString())
}
