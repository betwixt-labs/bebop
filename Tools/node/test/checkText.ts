import {checkText} from "../lib"
import path = require("path")
import fs = require("fs")

// okay so it's not exactly a test yet, but I'm using this to see what the output looks like

async function testCheck() {
    const fileText = fs.readFileSync(path.resolve(__dirname, "./invalid.bop"), "utf8")
    console.log(await checkText(fileText))
}

testCheck()
