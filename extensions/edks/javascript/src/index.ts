import { commitResult, readContext } from "./kernel";

export function chordCompile() {
  const input = readContext();
  commitResult(input + " \n hello world");
}
