const child_process = require('child_process')
const path = require('path')
const toolsDir = path.resolve(__dirname, '../tools')

if (process.platform === "darwin") {
    const executable = path.resolve(toolsDir, 'macos/bebopc')
    child_process.execSync("chmod", ['+x', executable], {stdio: 'ignore'})
    try {
        child_process.execSync('xattr', ['-d', 'com.apple.quarantine', executable], {stdio: 'ignore'})
    }
    catch (e) {
        // it's okay if this exits with an error status
    }
}
