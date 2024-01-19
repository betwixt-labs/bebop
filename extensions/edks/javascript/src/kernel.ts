interface JavyBuiltins {
  IO: {
    // readSync: Similar to `write` in POSIX
    //
    // Params:
    // - fd: File Descriptor (0 = stdin, 1 = stdout, 2 = stderr, >2 = custom)
    // - buffer: Buffer to read into
    //
    // Return:
    //   - > 0: Number of bytes read
    //   - = 0: EOF reached
    //   - < 0: Error occured
    readSync(fd: number, buffer: Uint8Array): number;
    // writeSync: Similar to `write` in POSIX
    //
    // Params:
    // - fd: File Descriptor (0 = stdin, 1 = stdout, 2 = stderr, >2 = custom)
    // - buffer: Buffer to write
    //
    // Return:
    //   - >= 0: Number of bytes written
    //   - < 0: Error occured
    writeSync(fd: number, buffer: Uint8Array): number;
  };
}

declare global {
  const Javy: JavyBuiltins;
}

const enum STDIO {
  StdIn,
  StdOut,
  StdErr,
}

const decoder = new TextDecoder();
const encoder = new TextEncoder();

function readFileSync(fd: number): Uint8Array {
  let buffer = new Uint8Array(1024);
  let bytesUsed = 0;
  while (true) {
    const bytesRead = Javy.IO.readSync(fd, buffer.subarray(bytesUsed));
    // A negative number of bytes read indicates an error.
    if (bytesRead < 0) {
      // FIXME: Figure out the specific error that occured.
      throw Error("Error while reading from file descriptor");
    }
    // 0 bytes read means we have reached EOF.
    if (bytesRead === 0) {
      return buffer.subarray(0, bytesUsed + bytesRead);
    }

    bytesUsed += bytesRead;
    // If we have filled the buffer, but have not reached EOF yet,
    // double the buffers capacity and continue.
    if (bytesUsed === buffer.length) {
      const nextBuffer = new Uint8Array(buffer.length * 2);
      nextBuffer.set(buffer);
      buffer = nextBuffer;
    }
  }
}

function writeFileSync(fd: number, buffer: Uint8Array) {
  while (buffer.length > 0) {
    // Try to write the entire buffer.
    const bytesWritten = Javy.IO.writeSync(fd, buffer);
    // A negative number of bytes written indicates an error.
    if (bytesWritten < 0) {
      throw Error("Error while writing to file descriptor");
    }
    // 0 bytes means that the destination cannot accept additional bytes.
    if (bytesWritten === 0) {
      throw Error("Could not write all contents in buffer to file descriptor");
    }
    // Otherwise cut off the bytes from the buffer that
    // were successfully written.
    buffer = buffer.subarray(bytesWritten);
  }
}

export const readContext = () => {
  const input = decoder.decode(readFileSync(STDIO.StdIn));
  return JSON.parse(input);
};

export const commitResult = (result: string) => {
  const data = encoder.encode(result);
  writeFileSync(STDIO.StdOut, data);
};

export const writeStandardError = (error: string) => {
  const data = encoder.encode(error);
  writeFileSync(STDIO.StdErr, data);
};
