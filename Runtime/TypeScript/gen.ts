import { spawn } from "child_process";
import * as fs from "fs/promises";

function buildSchema(source: string, dest: string): Promise<void> {
  const child = spawn(
    "dotnet",
    ["run", "--project", "../../Compiler", "--ts", dest, "--files", source],
    { stdio: ["ignore", "inherit", "inherit"] }
  );

  return new Promise((resolve, reject) => {
    child.once("close", resolve);
    child.once("error", reject);
  });
}

(async () => {
  await buildSchema(
    "../../Core/Schemas/RpcDatagram.bop",
    "src/generated/datagram.ts.tmp"
  );

  // not the most efficient thing ever, but probably fine for compile time to
  // just read all into memory, modify, and write all back out.

  const gen = await fs.open("src/generated/datagram.ts.tmp", "r");
  let data = await gen.readFile({ encoding: "utf8" });
  gen.close().then(async () => {
    await fs.rm("src/generated/datagram.ts.tmp");
  });

  data = data.replace(/from "bebop"/g, 'from ".."');

  await fs.rm("src/generated/datagram.ts", { force: true });
  let out = await fs.open("src/generated/datagram.ts", "w");
  await out.write(data);
  await out.close();
})();
