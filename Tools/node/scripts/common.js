const path = require('path')
const fs = require('fs');
const child_process = require('child_process')
const supportedCpuArchitectures = ['x64', 'arm64'];
const supportedPlatforms = ['win32', 'darwin', 'linux']


/**
 * Ensures the current host OS is supported by the Bebop compiler
 * @param {NodeJS.Architecture} arch the host arch
 * @param {NodeJS.Platform} platform  the host os
 */
function ensureHostSupport(arch, platform) {
    if (!supportedCpuArchitectures.includes(arch)) throw new Error(`Unsupported CPU arch: ${arch}`);
    if (!supportedPlatforms.includes(platform)) throw new Error(`Unsupported platform: ${platform}`);
}
/**
 * Gets information about the current compiler host
 * @returns 
 */
function getHostInfo() {
    const arch = process.arch;
    const platform = process.platform;
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
    return { arch: arch, os: osName, exeSuffix: osName === "windows" ? '.exe' : '' };

}
/**
 * Gets the fully qualified and normalized path to correct bundled version of the Bebop compiler
 */
const resolveBebopcPath = () => {
    const toolsDir = path.resolve(__dirname, '../tools')
    if (!fs.existsSync(toolsDir)) {
        throw new Error(`The root 'tools' directory does not exist: ${toolsDir}`);
    }
    const info = getHostInfo();
    const executable = path.normalize(`${path.resolve(toolsDir, `${info.os}/${info.arch}/bebopc${info.exeSuffix}`)}`);
    if (!fs.existsSync(executable)) {
        throw new Error(`${executable} does not exist`);
    }
    return executable;
}
/**
 * Ensures that bebopc binary is executable
 * @param {string} executable the path to the executable 
 */
const setExecutableBit = (executable) => {
    if (process.platform === "win32") {
        child_process.execSync(`Unblock-File -Path "${executable}"`, { stdio: 'ignore', shell: "powershell.exe" })
    } else {
        child_process.execSync(`chmod +x "${executable}"`, { stdio: 'ignore' })
    }
}

module.exports = { resolveBebopcPath, setExecutableBit };