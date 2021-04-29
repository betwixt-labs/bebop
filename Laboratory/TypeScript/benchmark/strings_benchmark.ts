import { IArrayOfStrings, ArrayOfStrings } from '../test/generated/gen';
import benchmark = require('benchmark');
import { BebopView } from 'bebop';
const suite = new benchmark.Suite();

for (const length of [400, 380, 360, 340, 320, 300, 280, 260, 240, 220, 200]) {
    const m = { strings: ['ab', 'cd', 'ef', 'gh', 'ij'].map(x => x.repeat(length/2)) };
    const bm = ArrayOfStrings.encode(m);
    const jm = JSON.stringify(m);
    ArrayOfStrings.decode(bm).strings[4][0]; // as a test

    suite.add(`Bebop decode ${length} byte string (manual)`, function () {
        BebopView.getInstance().minimumTextDecoderLength = 999999999; // never use it
        ArrayOfStrings.decode(bm).strings[4][0];
    })
    suite.add(`Bebop decode ${length} byte string (TextD.)`, function () {
        BebopView.getInstance().minimumTextDecoderLength = 0; // always use it
        ArrayOfStrings.decode(bm).strings[4][0];
    })
    suite.add(`JSON decode ${length} byte string`, function () { JSON.parse(jm).strings[4][0]; })

    suite.add(`Bebop encode ${length} byte string (manual)`, function () {
        ArrayOfStrings.encode(m);
    })
    suite.add(`JSON stringify ${length} byte string`, function () { JSON.stringify(m); })
}

suite.on('cycle', cycle)
suite.run()
function cycle (e) {
  console.log(e.target.toString())
}
