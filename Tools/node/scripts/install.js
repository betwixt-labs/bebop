const child_process = require('child_process')
const path = require('path')
const toolsDir = path.resolve(__dirname, '../tools')

if (process.platform === "darwin") {
    const executable = path.resolve(toolsDir, 'macos/bebopc')
    child_process.execSync(`chmod +x "${executable}"`, {stdio: 'ignore'})
}
else if (process.platform === "linux") {
    const executable = path.resolve(toolsDir, 'linux/bebopc')
    child_process.execSync(`chmod +x "${executable}"`, {stdio: 'ignore'})
}
else if (process.platform === "win32") {
    const executable = path.resolve(toolsDir, 'windows/bebopc.exe')
    child_process.execSync(`Unblock-File -Path "${executable}"`, {stdio: 'ignore', shell: "powershell.exe"})
}
