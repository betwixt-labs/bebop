import child_process = require('child_process')
import {promisify} from 'util'
import path = require('path')
const exec = promisify(child_process.exec)

// const bebopc = path.resolve(__dirname, "../bebopc.js")


/** Ensure that the bebopc executable can be executed. */
export function ensurePermissions() {
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
}

interface BebopcCheckResponse {
    Message: string
    Severity: "warning" | "error"
    Span?: {
        FileName: string
        StartLine: number
        EndLine: number
        StartColumn: number
        EndColumn: number
    }
}

export type CheckResults = {error: true, issues: Issue[]} | {error: false}

export interface Issue {
    severity: string
    startLine: number
    startColumn: number
    endLine: number
    endColumn: number
    description: string
    fileName: string
}

function parseBebopcCheckResponse(stderr: string): Issue[] {
    const response = JSON.parse(stderr.trim().replace(/\\/g, "\\"))
    const issues = Array.isArray(response) ? response as BebopcCheckResponse[] : [response as BebopcCheckResponse];
    return issues.map((issue) => {
        const { Message, Severity, Span } = issue
        return {
            severity: Severity,
            description: Message,
            startLine: Span?.StartLine ?? 0,
            endLine: Span?.EndLine ?? 0,
            startColumn: Span?.StartColumn ?? 0,
            endColumn: Span?.EndColumn ?? 0,
            fileName: Span?.FileName ?? ""
        }
    })
}

function getBebopCompilerPath() {
    const toolsDir = path.resolve(__dirname, '../tools')
    if (process.platform === "win32") {
        return path.resolve(toolsDir, "windows/bebopc.exe")
    }
    else if (process.platform === "darwin") {
        return path.resolve(toolsDir, "macos/bebopc")
    }
    else if (process.platform === "linux") {
        return path.resolve(toolsDir, "linux/bebopc")
    }
    throw new Error("Unsupported operating system.")
}

const bebopc = getBebopCompilerPath()

checkProject({config: ""})

/** Validate entire project, providing either a directory to search for bebop.json or a path to a config. */
export async function checkProject(cfg:
    {directory: string, config?: never} |
    {directory?: never, config: string}
): Promise<CheckResults> {
    const processConfig: {cwd?: string} = {}
    if (cfg.directory) {
        processConfig.cwd = cfg.directory
    }
    let commandExtension = ""
    if (cfg.config) {
        commandExtension = `--config ${cfg.config}`
    }
    return new Promise((resolve, reject) => {
        child_process.exec(
            `${bebopc} --check --log-format JSON ${commandExtension}`,
            processConfig,
            (error, stdout, stderr) => {
                if (stderr.trim().length > 0) {
                    resolve({
                        error: true,
                        issues: parseBebopcCheckResponse(stderr)
                    })
                }
                resolve({error: false})
            }
        )
    })
}

/** Validate schema file passed by path. */
export async function check(path: string): Promise<CheckResults> {
    return new Promise((resolve, reject) => {
        child_process.exec(`${bebopc} --check ${path} --log-format JSON `, (error, stdout, stderr) => {
            if (stderr.trim().length > 0) {
                resolve({error: true, issues: parseBebopcCheckResponse(stderr)})
            }
            else {
                resolve({error: false})
            }
        })
    })
}

/** Validate schema passed as string. */
export async function checkSchema(contents: string): Promise<{error: boolean, issues?: Issue[]}> {
    return new Promise((resolve, reject) => {
        const compiler = child_process.spawn(bebopc, ['--log-format', 'JSON', '--check-schema'])
        let stderr: string = ""
        compiler.stderr.on('data', (data: string) =>{
            stderr += data
        })
        compiler.on("close", (code: number) => {
            if (stderr.trim().length > 0) {
                resolve({error: true, issues: parseBebopcCheckResponse(stderr)})
            }
            else {
                resolve({error: false})
            }
        })
        compiler.stdin.write(contents)
        compiler.stdin.end()
    })
}
