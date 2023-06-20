import benchmark = require("benchmark");
import * as G from "../test/generated/gen";
const suite = new benchmark.Suite();

const song = new G.Song({
  title: "The Song",
  year: 1996,
  performers: [
    {
      name: "The Performer",
      plays: G.Instrument.Sax,
    },
  ],
});

const encodedJsonLib =  song.toJson();
const encodedBinaryLib = song.encode();
const standardJson = JSON.stringify(song);
console.log(standardJson)
console.log(encodedJsonLib)

suite.add(`encode binary bebop`, () => {
   G.Song.encode(song);
});
suite.add(`encode json-over-bebop`, function () {
   G.Song.encodeToJson(song);
});

suite.add(`encode standard json`, function () {
    JSON.stringify(song);
});

suite.add(`decode binary bebop`, () => {
   G.Song.decode(encodedBinaryLib)
});

suite.add(`decode json-over-bebop`, function () {
  G.Song.fromJson(encodedJsonLib);
});

suite.add(`decode standard json`, function () {
    JSON.parse(standardJson);
});

suite.on("cycle", cycle);
suite.run();
function cycle(e) {
  console.log(e.target.toString());
}
