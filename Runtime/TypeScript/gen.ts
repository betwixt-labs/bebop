// dotnet run --project ../../Compiler --ts "generated/datagram.ts" --files ../../Core/Schemas/RpcDatagram.bop

async function buildSchema(source: string, dest: string): Promise<void> {}

async function replaceInFile(
  path: string,
  o: RegExp,
  n: string
): Promise<void> {}

(async () => {
  await buildSchema(
    "../../Core/Schemas/RpcDatagram.bop",
    "generated/datagram.ts"
  );
  await replaceInFile("generated/datagram.ts", /from "bebop"/, 'from ".."');
})();
