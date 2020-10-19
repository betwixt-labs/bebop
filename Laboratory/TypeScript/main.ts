import { PierogiView } from './Generated/PierogiView';
import { lab } from './Generated/lab';

const bytes = lab.S.encode({ i: BigInt(1234567) });
console.log(bytes);
console.log(lab.S.decode(bytes));

