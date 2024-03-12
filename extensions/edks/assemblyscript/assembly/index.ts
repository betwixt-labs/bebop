import {
  deserializeGeneratorContext,
  getBebopcVersion,
  writeError,
  writeLine,
} from "./std";

// @ts-ignore: decorator
@exportAs("chord_compile")
export function compile(context: string): string {
  const generatorContext = deserializeGeneratorContext(context);
  writeLine("Hello from assemblyscript!");
  writeError("This is an error from assemblyscript!");
  writeLine("BebopC version: " + getBebopcVersion());
  return context;
}
