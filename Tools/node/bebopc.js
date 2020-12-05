#!/usr/bin/env node

const child_process = require('child_process')
const path = require('path')
const toolsDir = path.resolve(__dirname, 'tools')

const [, , ...args] = process.argv
let executable

if (process.platform === "win32") {
    executable = path.resolve(toolsDir, "windows/bebopc.exe")
}
else if (process.platform === "darwin") {
    executable = path.resolve(toolsDir, "macos/bebopc")
}
else if (process.platform === "linux") {
    executable = path.resolve(toolsDir, "linux/bebopc")
}

try {
    child_process.execFileSync(executable, args, {stdio: "inherit"})
    process.exit(0)
}
catch (e) {
    process.exit(e.status)
}
