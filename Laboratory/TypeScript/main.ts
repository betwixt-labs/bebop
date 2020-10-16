import { PierogiView } from './Generated/PierogiView';
import { lab } from './Generated/lab';

var view = new PierogiView();
lab.S.encode({ s: 'あé😊3' }, view);
console.log(view.toArray());
console.log(lab.S.decode(view));
