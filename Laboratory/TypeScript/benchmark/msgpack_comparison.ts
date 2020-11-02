import { IMsgpackComparison, MsgpackComparison } from '../test/generated/msgpack_comparison';
import benchmark = require('benchmark');
import msgpack = require("@msgpack/msgpack");
const suite = new benchmark.Suite();

const m: IMsgpackComparison = {
    "iNT0": 0,
    "iNT1": 1,
    "iNT1_": -1,
    "iNT8": 255,
    "iNT8_": -255,
    "iNT16": 256,
    "iNT16_": -256,
    "iNT32": 65536,
    "iNT32_": -65536,
    // "nil": null,
    "tRUE": true,
    "fALSE": false,
    "fLOAT": 0.5,
    "fLOAT_": -0.5,
    "sTRING0": "",
    "sTRING1": "A",
    "sTRING4": "foobarbaz",
    "sTRING8": "Omnes viae Romam ducunt.",
    "sTRING16": "L’homme n’est qu’un roseau, le plus faible de la nature ; mais c’est un roseau pensant. Il ne faut pas que l’univers entier s’arme pour l’écraser : une vapeur, une goutte d’eau, suffit pour le tuer. Mais, quand l’univers l’écraserait, l’homme serait encore plus noble que ce qui le tue, puisqu’il sait qu’il meurt, et l’avantage que l’univers a sur lui, l’univers n’en sait rien. Toute notre dignité consiste donc en la pensée. C’est de là qu’il faut nous relever et non de l’espace et de la durée, que nous ne saurions remplir. Travaillons donc à bien penser : voilà le principe de la morale.",
    "aRRAY0": [],
    "aRRAY1": [ "foo" ],
    "aRRAY8": [ 1, 2, 4, 8, 16, 32, 64, 128, 256, 512, 1024, 2048, 4096, 8192, 16384, 32768, 65536, 131072, 262144, 524288, 1048576 ],
    // "map0": {},
    // "map1": { "foo": "bar" }
};

const bm = MsgpackComparison.encode(m);
const jm = JSON.stringify(m);
const mm = msgpack.encode(m);

suite.add('  Bebop encode', () => MsgpackComparison.encode(m));
suite.add('  Bebop decode', () => MsgpackComparison.decode(bm));
suite.add('   JSON encode', () => JSON.stringify(m));
suite.add('   JSON decode', () => JSON.parse(jm));
suite.add('msgpack encode', () => msgpack.encode(m));
suite.add('msgpack decode', () => msgpack.decode(mm));
suite.on('cycle', (e: any) => console.log(e.target.toString()))
suite.run()
