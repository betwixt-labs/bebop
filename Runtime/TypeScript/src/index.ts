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
// export * as rpc from "./rpc";
