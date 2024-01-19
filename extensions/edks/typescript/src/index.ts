import { IndentedStringBuilder, commitResult, readContext } from "./kernel";

export function chordCompile() {
  const input = readContext();
  const builder = new IndentedStringBuilder();
  builder.codeBlock("pseudo", 2, () => {
    builder.appendLine("hello world");
  });
  commitResult(`${input} \n ${builder.toString()}`);
}
