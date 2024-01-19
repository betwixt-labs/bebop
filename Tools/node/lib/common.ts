import path from "path";
import fs from "fs";
import os from "os";
import child_process from "child_process";
import nodeBindings from "wasi-js/dist/bindings/node";
import { WASIExitError, WASIKillError } from "wasi-js/dist/types";
import WASI from "wasi-js";

const supportedCpuArchitectures = ["x64", "arm64"];
const supportedPlatforms = ["win32", "darwin", "linux"];

const isWebContainer = (() => {
  return true;
  const isStackblitz =
    process.env.SHELL === "/bin/jsh" && process.versions.webcontainer != null;
  if (isStackblitz) {
    return true;
  }
  // codesandbox
  if (process.env.CSB?.includes("true")) {
    return true;
  }
  return false;
})();

/**
 * Ensures the current host OS is supported by the Bebop compiler
 * @param {NodeJS.Architecture} arch the host arch
 * @param {NodeJS.Platform} platform  the host os
 */
function ensureHostSupport(arch: string, platform: string) {
  if (isWebContainer) return;
  if (!supportedCpuArchitectures.includes(arch))
    throw new Error(`Unsupported CPU arch: ${arch}`);
  if (!supportedPlatforms.includes(platform))
    throw new Error(`Unsupported platform: ${platform}`);
}
/**
 * Gets information about the current compiler host
 * @returns
 */
function getHostInfo() {
  const arch = process.arch;
  const platform = process.platform;
  if (isWebContainer) {
    return {
      arch: "wasm",
      os: "wasi",
      exeSuffix: ".wasm",
    };
  }
  ensureHostSupport(arch, platform);
  const osName = (() => {
    switch (platform) {
      case "win32":
        return "windows";
      case "darwin":
        return "macos";
      case "linux":
        return "linux";
      default:
        throw new Error(`Unknown platform name: ${platform}`);
    }
  })();
  return {
    arch: arch,
    os: osName,
    exeSuffix: osName === "windows" ? ".exe" : "",
  };
}
/**
 * Gets the fully qualified and normalized path to correct bundled version of the Bebop compiler
 */
const resolveBebopcPath = () => {
  const toolsDir = path.resolve(__dirname, "../tools");
  if (!fs.existsSync(toolsDir)) {
    throw new Error(`The root 'tools' directory does not exist: ${toolsDir}`);
  }
  const info = getHostInfo();
  const executable = path.normalize(
    `${path.resolve(
      toolsDir,
      `${info.os}/${info.arch}/bebopc${info.exeSuffix}`
    )}`
  );
  if (!fs.existsSync(executable)) {
    throw new Error(`${executable} does not exist`);
  }
  return executable;
};
/**
 * Ensures that bebopc binary is executable
 * @param {string} executable the path to the executable
 */
const setExecutableBit = (executable: string) => {
  if (isWebContainer) return;
  if (process.platform === "win32") {
    child_process.execSync(`Unblock-File -Path "${executable}"`, {
      stdio: "ignore",
      shell: "powershell.exe",
    });
  } else {
    child_process.execSync(`chmod +x "${executable}"`, { stdio: "ignore" });
  }
};

const launchBebopc = async (args: string[]): Promise<number> => {
  if (!isWebContainer) {
    const executable = resolveBebopcPath();
    const child = child_process.spawn(executable, args, {
      stdio: "inherit",
    });
    return await new Promise((resolve, reject) => {
      child.on("exit", (code) => {
        if (code === 0) {
          resolve(code);
        } else {
          reject(new Error(`bebopc exited with code ${code}`));
        }
      });
    });
  } else {
    return await launchWasi(args);
  }
};

const decoder = new TextDecoder();

const launchWasi = async (args: string[]): Promise<number> => {
  // add the executable name to the front
  args.unshift("bebopc");
  const module = await WebAssembly.compile(
    await new Promise((resolve, reject) => {
      fs.readFile(resolveBebopcPath(), (err, data) => {
        if (err) {
          reject(err);
        } else {
          resolve(data);
        }
      });
    })
  );

  let sab: Int32Array | undefined;
  const workingDir = process.cwd();
  let standardOutput = "";
  let standardError = "";
  const wasi = new WASI({
    args,
    env: {
      RUST_BACKTRACE: "1",
    },
    bindings: {
      ...nodeBindings,
      exit: (code: number | null) => {
        throw new WASIExitError(code);
      },
      kill: (signal: string) => {
        throw new WASIKillError(signal);
      },
    },
    preopens: {
      // we're giving the wasi module access to the current working directory
      "/": workingDir,
      "/tmp": os.tmpdir(),
    },

    sendStdout: (data: Uint8Array): void => {
      standardOutput += decoder.decode(data);
    },
    sendStderr: (data: Uint8Array) => {
      standardError += decoder.decode(data);
    },
    sleep: (ms: number) => {
      sab ??= new Int32Array(new SharedArrayBuffer(4));
      Atomics.wait(sab, 0, 0, Math.max(ms, 1));
    },
  });

  let imports = wasi.getImports(module);
  imports = {
    wasi_snapshot_preview1: {
      ...imports.wasi_snapshot_preview1,
      sock_accept: () => -1,
    },
  };

  const instance = await WebAssembly.instantiate(module, imports);
  let exitCode = 0;
  try {
    wasi.start(instance);
  } catch (e) {
    if (e instanceof WASIExitError) {
      exitCode = e.code ?? 127;
    } else {
      throw e;
    }
  }
  standardOutput = standardOutput.trim();
  if (standardOutput) {
    console.log(standardOutput);
  }
  standardError = standardError.trim();
  if (standardError) {
    console.error(standardError);
  }
  return exitCode;
};

export { resolveBebopcPath, setExecutableBit, launchBebopc };
