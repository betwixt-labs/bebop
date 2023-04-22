const { resolveBebopcPath, setExecutableBit } = require("./common");

const bebopc = resolveBebopcPath();
setExecutableBit(bebopc)