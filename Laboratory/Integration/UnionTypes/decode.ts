import { Union1 } from "./union1";
import fs = require("fs");

const buffer = fs.readFileSync(process.argv[2]);
const u = Union1.decode(buffer);
if (u.discriminator === 2 && u.value.r === "Success") {
  process.exit(0);
} else {
  process.exit(1);
}
