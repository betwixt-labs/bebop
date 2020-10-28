import { IArrayOfStrings, ArrayOfStrings } from '../test/generated/array_of_strings';
import benchmark = require('benchmark');
import { BebopView } from '../test/generated/BebopView';
const suite = new benchmark.Suite();

for (const length of [10000, 1000, 360, 180, 100, 10]) {
    const m = { strings: ['ab', 'cd', 'ef', 'gh', 'ij'].map(x => x.repeat(length/2)) };
    const bm = ArrayOfStrings.encode(m);
    console.log([length, bm.length]);
    const jm = JSON.stringify(m);
    suite.add(`Bebop decode ${length} byte string (manual)`, function () {
        BebopView.getInstance().minimumTextDecoderLength = 999999999; // never use it
        ArrayOfStrings.decode(bm).strings[4][0];
    })
    suite.add(`Bebop decode ${length} byte string (TextDecoder)`, function () {
        BebopView.getInstance().minimumTextDecoderLength = 0; // always use it
        ArrayOfStrings.decode(bm).strings[4][0];
    })
    suite.add(`JSON decode ${length} byte string`, function () { JSON.parse(jm).strings[4][0]; })
}

suite.on('cycle', cycle)
suite.run()
function cycle (e) {
  console.log(e.target.toString())
}
