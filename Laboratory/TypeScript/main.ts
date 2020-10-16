import { PierogiView } from './Generated/PierogiView';
import { lab } from './Generated/lab';

var view = new PierogiView();
lab.S.encode({ s: 'あé😊3', g: 'defdefde-fdef-defd-efde-fdefdefdefde', f: 1.234 }, view);
console.log(lab.S.decode(view).g);

for (var j = 0; j < 4; j++) {
var t0 = Date.now();
for (var i = 0; i < 100000; i++) {
    var view = new PierogiView();
    lab.S.encode({ s: 'あé😊3', g: '01020304-0a0b-0a0b-aabb-ccddaabbccdd', f: 1.234 }, view);
}
var t1 = Date.now();
console.log(t1 - t0);
}
