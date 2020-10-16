import { PierogiView } from './Generated/PierogiView';
import { lab } from './Generated/lab';

var view = new PierogiView();
lab.M.encode({ x: 3, y: 4 }, view);
console.log(view.toArray());
console.log(lab.M.decode(view));
