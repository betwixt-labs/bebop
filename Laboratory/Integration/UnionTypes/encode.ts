import { Union1 } from "./union1";

const buffer = Union1.encode({ discriminator: 2, value: { r: "Success" } });
process.stdout.write(buffer);

