import * as fs from 'fs';
import * as path from 'path';
import * as https from 'https';
import * as zlib from 'zlib';
import * as os from 'os';
import { execSync } from 'child_process';

const JAVY_VERSION = "3.1.1";

interface PlatformInfo {
    os: string;
    arch: string;
    ext: string;
}

function getPlatformInfo(): PlatformInfo {
    const platform = os.platform();
    const arch = os.arch();

    let osName: string;
    let extension = '';
    let archName: string;

    switch (platform) {
        case 'win32':
            osName = 'windows';
            extension = '.exe';
            archName = 'x86_64'; // Always use x86_64 for Windows
            break;
        case 'darwin':
            osName = 'macos';
            archName = arch === 'arm64' ? 'arm' : 'x86_64';
            break;
        case 'linux':
            osName = 'linux';
            archName = arch === 'arm64' ? 'arm' : 'x86_64';
            break;
        default:
            throw new Error(`Unsupported platform: ${platform}`);
    }

    return { os: osName, arch: archName, ext: extension };
}

async function downloadFile(url: string, outputPath: string): Promise<void> {
    return new Promise((resolve, reject) => {
        const request = https.get(url, (response) => {
            if (response.statusCode === 302 || response.statusCode === 301) {
                // Handle redirect
                const newUrl = response.headers.location;
                if (!newUrl) {
                    reject(new Error('Redirect location not found'));
                    return;
                }
                downloadFile(newUrl, outputPath).then(resolve).catch(reject);
                return;
            }

            if (response.statusCode !== 200) {
                reject(new Error(`Failed to download file, status code: ${response.statusCode}`));
                return;
            }

            const fileStream = fs.createWriteStream(outputPath);
            response.pipe(fileStream);

            fileStream.on('finish', () => {
                fileStream.close();
                resolve();
            });
        }).on('error', (err) => {
            fs.unlink(outputPath, () => { }); // Delete the file async. (but we don't check the result)
            reject(err);
        });

        request.end();
    });
}

async function extractGzip(inputPath: string, outputPath: string): Promise<void> {
    return new Promise((resolve, reject) => {
        const inputStream = fs.createReadStream(inputPath);
        const outputStream = fs.createWriteStream(outputPath);
        const gunzip = zlib.createGunzip();

        inputStream
            .pipe(gunzip)
            .pipe(outputStream)
            .on('finish', () => {
                outputStream.close();
                resolve();
            })
            .on('error', (err) => {
                reject(err);
            });
    });
}

function isJavyInstalled(): boolean {
    try {
        execSync('./javy-cli --version', { stdio: 'ignore' });
        return true;
    } catch (error) {
        return false;
    }
}

async function main() {
    if (isJavyInstalled()) {
        console.log("Javy CLI is already installed. Skipping installation.");
        process.exit(0);
        return;
    }

    try {
        const { os, arch, ext } = getPlatformInfo();
        const filename = `javy-${arch}-${os}-v${JAVY_VERSION}.gz`;
        const url = `https://github.com/bytecodealliance/javy/releases/download/v${JAVY_VERSION}/${filename}`;
        const downloadedFile = path.join(process.cwd(), filename);
        const extractedFile = path.join(process.cwd(), filename.replace('.gz', ''));
        const finalFile = path.join(process.cwd(), `javy-cli${ext}`);

        console.log(`Downloading Javy CLI v${JAVY_VERSION} for ${os} (${arch})...`);
        await downloadFile(url, downloadedFile);

        console.log(`Extracting ${filename}...`);
        await extractGzip(downloadedFile, extractedFile);

        console.log(`Installing Javy CLI to ./javy-cli${ext}`);
        fs.renameSync(extractedFile, finalFile);

        // Clean up the downloaded .gz file
        fs.unlinkSync(downloadedFile);

        if (fs.existsSync(finalFile)) {
            // Make the file executable on Unix systems
            if (os !== 'windows') {
                console.log('Setting execute permissions for Javy CLI...');
                fs.chmodSync(finalFile, 0o755);
            }

            console.log(`Javy CLI v${JAVY_VERSION} has been successfully installed to ./javy-cli${ext}`);
            console.log(`You can now use it by running: ./javy-cli${ext}`);
            process.exit(0);
        } else {
            throw new Error(`Installation failed: javy-cli${ext} not found`);
        }
    } catch (error) {
        console.error("An error occurred:", error);
        console.log("Current directory contents:");
        fs.readdirSync(process.cwd()).forEach(file => {
            console.log(file);
        });
        process.exit(1);
    }
}

main();