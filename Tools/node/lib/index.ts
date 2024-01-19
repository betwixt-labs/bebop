import { launchBebopc, resolveBebopcPath, setExecutableBit } from "./common";

const args = process.argv.slice(2);

// use an IIFE to allow top-level await
(async () => {
  if (args[0] === "install") {
    setExecutableBit(resolveBebopcPath());
    process.exit(0);
  } else {
    try {
      process.exit(await launchBebopc(args));
    } catch (e) {
      console.error("Error running bebopc", e);
      process.exit(1);
    }
  }
})();
