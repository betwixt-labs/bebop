import { posix as path } from "node:path";

import WASI, { createFileSystem } from "wasi-js";
import browserBindings from "wasi-js/dist/bindings/browser";
import { WASIExitError, WASIFileSystem } from "wasi-js/dist/types";

import bebopcWasmUrl from "./bebopc.wasm?url";
import { WorkerResponse } from "./types";

const decoder = new TextDecoder();
let bebopcModule: WebAssembly.Module | undefined;

const getModule = async (callback: (loaded: number, total: number) => void) => {
  if (bebopcModule) return bebopcModule;
  const response = await fetch(bebopcWasmUrl);
  if (!response.ok) {
    throw new Error(`Failed to fetch bebopc.wasm: ${response.statusText}`);
  }
  const contentLength = response.headers.get("content-length");
  if (!contentLength) {
    throw new Error("Missing Content-Length header");
  }
  const total = Number.parseInt(contentLength, 10);
  let loaded = 0;

  const reader = response.body?.getReader();
  const chunks = new Uint8Array(total);
  let receivedLength = 0;

  if (reader) {
    while (true) {
      const { done, value } = await reader.read();
      if (done) break;

      if (value) {
        chunks.set(value, receivedLength);
        receivedLength += value.length;
        loaded += value.length;
        callback(loaded, total);
      }
    }
  }
  return (bebopcModule = WebAssembly.compile(chunks));
};

export async function runBebopc(
  files: Map<string, string>,
  args: string[],
  downloadProgressCallback: (loaded: number, total: number) => void
): Promise<WorkerResponse> {
  const fs = createFileSystem([
    {
      type: "mem",
      contents: Object.fromEntries(files),
    },
  ]);

  let stdout = "";
  let stderr = "";
  let sab: Int32Array | undefined;
  const wasi = new WASI({
    args,
    env: {
      RUST_BACKTRACE: "1",
      BEBOPC_LOG_FORMAT: "JSON",
    },
    // Workaround for bug in wasi-js; browser-hrtime incorrectly returns a number.
    bindings: {
      ...browserBindings,
      fs,
      hrtime: (...args): bigint => BigInt(browserBindings.hrtime(...args)),
    },
    preopens: {
      "/": "/",
    },

    sendStdout: (data: Uint8Array): void => {
      stdout += decoder.decode(data);
    },
    sendStderr: (data: Uint8Array) => {
      stderr += decoder.decode(data);
    },
    sleep: (ms: number) => {
      sab ??= new Int32Array(new SharedArrayBuffer(4));
      Atomics.wait(sab, 0, 0, Math.max(ms, 1));
    },
  });
  const module = await getModule(downloadProgressCallback);
  let imports = wasi.getImports(module);
  imports = {
    wasi_snapshot_preview1: {
      ...imports.wasi_snapshot_preview1,
      sock_accept: () => -1,
    },
  };
  const instance = await WebAssembly.instantiate(module, imports);
  let exitCode: number;
  try {
    wasi.start(instance);
    exitCode = 0;
  } catch (e) {
    if (e instanceof WASIExitError) {
      exitCode = e.code ?? 127;
    } else {
      return (e as any).toString();
    }
  }

  let output = "";

  for (const p of walk(fs, "/")) {
    if (files.has(p)) continue;
    output += `// @filename: ${p}\n`;
    output += fs.readFileSync(p, { encoding: "utf8" });
    output += "\n\n";
  }

  return {
    exitCode,
    stdErr: stderr,
    newFiles: output.trim(),
    stdOut: stdout,
  };
}

function* walk(fs: WASIFileSystem, dir: string): Generator<string> {
  for (const p of fs.readdirSync(dir)) {
    const entry = path.join(dir, p);
    const stat = fs.statSync(entry);
    if (stat.isDirectory()) {
      yield* walk(fs, entry);
    } else if (stat.isFile()) {
      yield entry;
    }
  }
}
