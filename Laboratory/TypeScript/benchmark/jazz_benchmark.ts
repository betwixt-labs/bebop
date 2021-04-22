import { ILibrary, Instrument, Library, ISong, Song, Musician } from '../test/generated/gen';
import benchmark = require('benchmark');
import msgpack = require("@msgpack/msgpack");
import Pbf = require('pbf');

// This file was generated with `pbf ./jazz.proto > ./jazz_protobuf.gen.js`,
// which is how `pbf` (the ProtoBuf implementation benchmarked here) works.
var PbfSong = require('./jazz_protobuf.gen').Song;

// I'd benchmark "Library", but it contains a Map(), which causes problems.
const song1: ISong = {
    title: 'Donna Lee',
    year: 1947,
    performers: [
        { name: 'Charlie Parker', plays: Instrument.Sax },
        { name: 'Miles Davis', plays: Instrument.Trumpet },
    ],
};

const bebopEncodedSong = Song.encode(song1);
const jsonEncodedSong = JSON.stringify(song1);
const msgpackEncodedSong = msgpack.encode(song1);
var modelPbf = new Pbf();
PbfSong.write(song1, modelPbf);
const protobufEncodedSong = modelPbf.finish();

const suite = new benchmark.Suite();

suite.add('Bebop   encode Song', () => Song.encode(song1));
suite.add('Bebop   decode Song', () => Song.decode(bebopEncodedSong));
suite.add('JSON    encode Song', () => JSON.stringify(song1));
suite.add('JSON    decode Song', () => JSON.parse(jsonEncodedSong));
suite.add('msgpack encode Song', () => msgpack.encode(song1));
suite.add('msgpack decode Song', () => msgpack.decode(msgpackEncodedSong));
suite.add('pbf     encode Song', () => { var p = new Pbf(); PbfSong.write(song1, p); p.finish(); });
suite.add('pbf     decode Song', () => PbfSong.read(new Pbf(protobufEncodedSong)));

suite.on('cycle', (e) => console.log(e.target.toString()));
suite.run();
