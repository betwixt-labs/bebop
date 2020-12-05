const child_process = require('child_process')
const path = require('path')
const toolsDir = path.resolve(__dirname, '../tools')

if (process.platform === "darwin") {
    const executable = path.resolve(toolsDir, 'macos/bebopc')
    child_process.execSync("chmod", ['+x', executable], {stdio: 'ignore'})
}
else if (process.platform == "linux") {
    const executable = path.resolve(toolsDir, 'linux/bebopc')
    child_process.execSync("chmod", ['+x', executable], {stdio: 'ignore'})
}
