import child_process = require('child_process')
import {promisify} from 'util'
import path = require('path')
const exec = promisify(child_process.exec)

// const bebopc = path.resolve(__dirname, "../bebopc.js")




interface BebopcCheckResponse {
    Message: string
    Span: {
        FileName: string
        StartLine: number
        EndLine: number
        StartColumn: number
        EndColumn: number
    }
}

export interface Issue {
    severity: string
    startLine: number
    startColumn: number
    endLine: number
    endColumn: number
    description: string
    fileName: string
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

/** Validate schema file passed by path. */
export async function check(path: string): Promise<{error: boolean, issues?: Issue[]}> {
    return new Promise((resolve, reject) => {
        child_process.exec(`${bebopc} --check ${path} --log-format JSON `, (error, stdout, stderr) => {
            if (stderr.trim().length > 0) {
                const { Message, Span }: BebopcCheckResponse = JSON.parse(stderr.trim().replace(/\\/g, "\\"))
                const issues: Issue[] = []
                issues.push({
                    severity: 'error',
                    description: Message,
                    startLine: Span.StartLine,
                    endLine: Span.EndLine,
                    startColumn: Span.StartColumn,
                    endColumn: Span.EndColumn,
                    fileName: Span.FileName
                })
                resolve({
                    error: true,
                    issues
                })
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
                const { Message, Span }: BebopcCheckResponse = JSON.parse(stderr.trim())
                const issues: Issue[] = []
                issues.push({
                    severity: 'error',
                    description: Message,
                    startLine: Span.StartLine,
                    endLine: Span.EndLine,
                    startColumn: Span.StartColumn,
                    endColumn: Span.EndColumn,
                    fileName: ""
                })
                resolve({
                    error: true,
                    issues
                })
            }
            else {
                resolve({error: false})
            }
        })
        compiler.stdin.write(contents)
        compiler.stdin.end()
    })
}
