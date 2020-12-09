"use strict";
var __awaiter = (this && this.__awaiter) || function (thisArg, _arguments, P, generator) {
    function adopt(value) { return value instanceof P ? value : new P(function (resolve) { resolve(value); }); }
    return new (P || (P = Promise))(function (resolve, reject) {
        function fulfilled(value) { try { step(generator.next(value)); } catch (e) { reject(e); } }
        function rejected(value) { try { step(generator["throw"](value)); } catch (e) { reject(e); } }
        function step(result) { result.done ? resolve(result.value) : adopt(result.value).then(fulfilled, rejected); }
        step((generator = generator.apply(thisArg, _arguments || [])).next());
    });
};
Object.defineProperty(exports, "__esModule", { value: true });
exports.checkText = exports.check = void 0;
const child_process = require("child_process");
const util_1 = require("util");
const path = require("path");
const exec = util_1.promisify(child_process.exec);
function getBebopCompilerPath() {
    const toolsDir = path.resolve(__dirname, '../tools');
    if (process.platform === "win32") {
        return path.resolve(toolsDir, "windows/bebopc.exe");
    }
    else if (process.platform === "darwin") {
        return path.resolve(toolsDir, "macos/bebopc");
    }
    else if (process.platform === "linux") {
        return path.resolve(toolsDir, "linux/bebopc");
    }
    throw new Error("Unsupported operating system.");
}
const bebopc = getBebopCompilerPath();
/** Validate schema file passed by path. */
function check(path) {
    return __awaiter(this, void 0, void 0, function* () {
        return new Promise((resolve, reject) => {
            child_process.exec(`${bebopc} --check ${path} --log-format JSON `, (error, stdout, stderr) => {
                if (stderr.trim().length > 0) {
                    console.log(stderr.trim());
                    const { Message, Span } = JSON.parse(stderr.trim().replace(/\\/g, "\\"));
                    const issues = [];
                    issues.push({
                        severity: 'error',
                        description: Message,
                        startLine: Span.StartLine,
                        endLine: Span.EndLine,
                        startColumn: Span.StartColumn,
                        endColumn: Span.EndColumn
                    });
                    resolve({
                        error: true,
                        issues
                    });
                }
                else {
                    resolve({ error: false });
                }
            });
        });
    });
}
exports.check = check;
/** Validate schema passed as string. */
function checkText(contents) {
    return __awaiter(this, void 0, void 0, function* () {
        return new Promise((resolve, reject) => {
            const compiler = child_process.spawn(bebopc, ['--log-format', 'JSON', '--check-schema']);
            let stderr = "";
            compiler.stderr.on('data', (data) => {
                stderr += data;
            });
            compiler.on("close", (code) => {
                if (stderr.trim().length > 0) {
                    console.log(stderr.trim().replace(/\\/g, "\\\\"));
                    const { Message, Span } = JSON.parse(stderr.trim().replace(/\\/g, "\\\\"));
                    const issues = [];
                    issues.push({
                        severity: 'error',
                        description: Message,
                        startLine: Span.StartLine,
                        endLine: Span.EndLine,
                        startColumn: Span.StartColumn,
                        endColumn: Span.EndColumn
                    });
                    resolve({
                        error: true,
                        issues
                    });
                }
                else {
                    resolve({ error: false });
                }
            });
            compiler.stdin.write(contents);
            compiler.stdin.end();
        });
    });
}
exports.checkText = checkText;
//# sourceMappingURL=index.js.map