import {check} from "../lib"
import path = require("path")

// okay so it's not exactly a test yet, but I'm using this to see what the output looks like

async function testCheck() {
    console.log(await check(path.resolve(__dirname, "./invalid.bop")))
}

testCheck()
