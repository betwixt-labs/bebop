import benchmark = require("benchmark");
import * as G from "../test/generated/gen";
import { Guid, GuidMap } from "bebop";
const suite = new benchmark.Suite();

const lib: G.ILibrary = {
  songs: new GuidMap([
    [
      Guid.parseGuid("01234567-0123-0123-0123-0123456789ab"),
      {
        title: "The Song",
        artist: "The Artist",
        performers: [
          {
            name: "The Performer",
            plays: G.Instrument.Sax,
          },
        ],
      },
    ],
  ]),
};

const encodedJsonLib = G.Library.encodeToJson(lib);
const encodedBinaryLib = G.Library.encode(lib);

suite.add(`encode binary bebop`, () => {
    G.Library.encode(lib);
});
suite.add(`encode json-over-bebop`, function () {
  G.Library.encodeToJson(lib);
});

suite.add(`decode binary bebop`, () => {
    G.Library.decode(encodedBinaryLib);
});

suite.add(`decode json-over-bebop`, function () {
  G.Library.fromJson(encodedJsonLib);
});

suite.on('cycle', cycle)
suite.run()
function cycle(e) {
    console.log(e.target.toString())
}