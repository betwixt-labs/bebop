import { IMsgpackComparison, MsgpackComparison } from '../test/generated/gen';
import benchmark = require('benchmark');
import msgpack = require("@msgpack/msgpack");
const suite = new benchmark.Suite();

const m: IMsgpackComparison = {
    "ant0": 0,
    "ant1": 1,
    "ant1x": -1,
    "ant8": 255,
    "ant8x": -255,
    "ant16": 256,
    "ant16x": -256,
    "ant32": 65536,
    "ant32x": -65536,
    // "nil": null,
    "arue": true,
    "aalse": false,
    "aloat": 0.5,
    "aloatx": -0.5,
    "atring0": "",
    "atring1": "A",
    "atring4": "foobarbaz",
    "atring8": "Omnes viae Romam ducunt.",
    "atring16": "L’homme n’est qu’un roseau, le plus faible de la nature ; mais c’est un roseau pensant. Il ne faut pas que l’univers entier s’arme pour l’écraser : une vapeur, une goutte d’eau, suffit pour le tuer. Mais, quand l’univers l’écraserait, l’homme serait encore plus noble que ce qui le tue, puisqu’il sait qu’il meurt, et l’avantage que l’univers a sur lui, l’univers n’en sait rien. Toute notre dignité consiste donc en la pensée. C’est de là qu’il faut nous relever et non de l’espace et de la durée, que nous ne saurions remplir. Travaillons donc à bien penser : voilà le principe de la morale.",
    "array0": [],
    "array1": [ "foo" ],
    "array8": [ 1, 2, 4, 8, 16, 32, 64, 128, 256, 512, 1024, 2048, 4096, 8192, 16384, 32768, 65536, 131072, 262144, 524288, 1048576 ],
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
