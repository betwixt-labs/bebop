if (require && !TextDecoder) {
  // conditional `require` to support nodejs and browser
  /* eslint-disable-next-line @typescript-eslint/no-var-requires */
  global.TextDecoder = require("util").TextDecoder;
}

export class BebopRuntimeError extends Error {
  constructor(message: string) {
    super(message);
    this.name = "BebopRuntimeError";
  }
}

export { BebopView } from "./view";
export { BebopRecordImpl, BebopUnionRecordImpl } from "./record";
export * from "./rpc";

/**
 * Use this in the `default` statement of a switch to ensure all cases are handled.
 *
 * @ignore
 */
export function assertUnreachable(x: never): never {
  throw new Error(`Didn't expect to get here ${x}`);
}
