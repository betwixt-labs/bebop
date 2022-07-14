#!/usr/bin/env node

const child_process = require('child_process')
const { resolveBebopcPath } = require('./scripts/common')

const args = process.argv.slice(2)

const bebopc = resolveBebopcPath();

try {
    child_process.execFileSync(bebopc, args, { stdio: "inherit" })
    process.exit(0)
}
catch (e) {
    process.exit(e.status)
}
