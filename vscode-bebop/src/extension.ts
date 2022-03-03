'use strict';

import * as vscode from 'vscode';
import { workspace, Disposable, ExtensionContext } from 'vscode';
import * as lsp from "vscode-languageclient/node";
import { Trace } from 'vscode-jsonrpc';
import * as path from "path";
import { existsSync } from "fs";
import { platform } from "os";

export async function activate(context: vscode.ExtensionContext) {
    // Register our own little status bar
    let statusBar = createStatusBar(context);
    statusBar.text = '$(sync~spin) Starting Bebop...';

    // Start the language server
    let client = startLanguageServer(context);
    if (client !== null) {
        // Wait until the language server is ready
        await client.onReady();
    }

    // Hide the status bar
    statusBar.hide();
}

function createStatusBar(context: vscode.ExtensionContext): vscode.StatusBarItem {
    let statusBar = vscode.window.createStatusBarItem(vscode.StatusBarAlignment.Right, 100);
    context.subscriptions.push(statusBar);
    statusBar.command = 'bebop.status';
    statusBar.show();
    return statusBar;
}

function startLanguageServer(context: vscode.ExtensionContext): lsp.LanguageClient | null {
    // Get the Bebop compiler path
    let executable = getBebopCompilerPath(context);
    if (!existsSync(executable)) {
        vscode.window.showErrorMessage(`Bebop compiler was not found at: ${executable}`);
        return null;
    }

    let serverOptions: lsp.ServerOptions = {
        run: { command: executable, args: ['--langserv'] },
        // debug: { command: serverExe, args: ['--langserv', '--debug'] }
        debug: { command: executable, args: ['--langserv'] }
    };

    let clientOptions: lsp.LanguageClientOptions = {
        documentSelector: [
            {
                pattern: '**/*.bop',
            }
        ],
        synchronize: {
            configurationSection: 'bebopLanguageServer',
            fileEvents: workspace.createFileSystemWatcher('**/*.bop')
        },
    };

    // Create the language client and start the client.
    const client = new lsp.LanguageClient('bebopLanguageServer', 'Bebop Language Server', serverOptions, clientOptions);
    client.trace = Trace.Verbose;
    let disposable = client.start();

    // Push the disposable to the context's subscriptions so that the
    // client can be deactivated on extension deactivation
    context.subscriptions.push(disposable);

    return client;
}

function getBebopCompilerPath(context: vscode.ExtensionContext) {
    // Got an environment variable?
    let envPath = process.env.BEBOP_LANGUAGE_SERVER_PATH;
    if (envPath !== undefined) {
        return envPath;
    }

    // In development mode?
    if (context.extensionMode === vscode.ExtensionMode.Development) {
        // Resolve the Bebop compiler from the bin directory
        return path.resolve(path.join(context.extensionPath, `../bin/${getCompilerPlatformPath(true)}`));
    }

    // Use the packaged compiler
    return context.asAbsolutePath(getCompilerPlatformPath());
}

function getCompilerPlatformPath(debug: boolean = false) {
    switch (platform()) {
        case "win32":
            if (debug) {
                return "compiler/Debug/artifacts/bebopc.exe";
            } else {
                return "bebopc/windows/bebopc.exe";
            }
        case "darwin":
            if (debug) {
                return "compiler/Debug/artifacts/bebopc";
            } else {
                return "bebopc/macos/bebopc";
            }
        default:
            if (debug) {
                return "compiler/Debug/artifacts/bebopc";
            } else {
                return "bebopc/linux/bebopc";
            }
    }
}

export function deactivate() { }
