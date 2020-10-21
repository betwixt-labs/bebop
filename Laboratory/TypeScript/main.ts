import { PierogiView } from './Generated/PierogiView';
import { lab } from './Generated/lab';
import b from 'benny';

var arr: Uint8Array;
const ints = new Array(1000).fill(0).map(() => (Math.random() - 0.5) * 100000 | 0);
const bigints = new Array(1000).fill(0).map(() => BigInt((Math.random() - 0.5) * 100000 | 0));
const f32s = new Float32Array(ints);
const f64s = new Float64Array(ints);
const randomBytes = new Uint8Array(16000);
for (var i = 0; i < randomBytes.length; i++) {
    randomBytes[i] = Math.random() * 256 | 0;
}
const mediaMessage = { codec: lab.VideoCodec.H265, data: { time: 1.2345, width: 2000, height: 1000, fragment: randomBytes } }

b.suite(
    "Encoding/decoding arrays",
    b.add("Encoding MediaMessage", () => { arr = lab.MediaMessage.encode(mediaMessage); }),
    b.add("Decoding MediaMessage", () => { lab.MediaMessage.decode(arr); }),
    // b.add("Encoding a thousand int32s", () => { arr = lab.Int32s.encode({ a: ints }); }),
    // b.add("Decoding a thousand int32s", () => { lab.Int32s.decode(arr); }),
    // b.add("Encoding a thousand int64s", () => { arr = lab.Int64s.encode({ a: bigints }); }),
    // b.add("Decoding a thousand int64s", () => { lab.Int64s.decode(arr); }),
    // b.add("Encoding a thousand float32s", () => { arr = lab.Float32s.encode({ a: f32s }); }),
    // b.add("Decoding a thousand float32s", () => { lab.Float32s.decode(arr); }),
    // b.add("Encoding a thousand float64s", () => { arr = lab.Float64s.encode({ a: f64s }); }),
    // b.add("Decoding a thousand float64s", () => { lab.Float64s.decode(arr); }),
    b.cycle(),
    b.complete(),
    b.save({ file: "reduce", version: "1.0.0" }),
    b.save({ file: "reduce", format: "chart.html" })
);

