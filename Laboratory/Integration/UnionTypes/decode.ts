import { Union1, Right } from "./union1";
import fs = require("fs");

const buffer = fs.readFileSync(process.argv[2]);
const u = Union1.decode(buffer);
if (u.discriminator === Right.discriminator && u.value.r === "Success") {
  process.exit(0);
} else {
  process.exit(1);
}
