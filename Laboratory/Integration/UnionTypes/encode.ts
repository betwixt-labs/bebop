import { Union1, Right } from "./union1";

const buffer = Union1.encode({ discriminator: Right.discriminator, value: { r: "Success" } });
process.stdout.write(buffer);

