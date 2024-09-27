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
  return JSON.parse(input) as CompilerContext;
};

export const commitResult = (result: string) => {
  const data = encoder.encode(result);
  writeFileSync(STDIO.StdOut, data);
};

export const writeStandardError = (error: string) => {
  const data = encoder.encode(error);
  writeFileSync(STDIO.StdErr, data);
};

export class IndentedStringBuilder {
  private spaces: number;
  private builder: string[];

  constructor(spaces: number = 0) {
    this.spaces = spaces;
    this.builder = [];
  }

  append(text: string): IndentedStringBuilder {
    const indent = " ".repeat(this.spaces);
    const lines = text.split("\n");
    const indentedLines = lines.map((x) => indent + x.trimEnd());
    const indentedText = indentedLines.join("\n").trimEnd();
    this.builder.push(indentedText);
    return this;
  }

  appendMid(text: string): IndentedStringBuilder {
    if (text.split("\n").length > 1) {
      throw new Error("AppendMid must not contain multiple lines");
    }

    this.builder.push(text);
    return this;
  }

  appendEnd(text: string): IndentedStringBuilder {
    if (text.split("\n").length > 1) {
      throw new Error("AppendEnd must not contain multiple lines");
    }

    this.builder.push(text.trimEnd() + "\n");
    return this;
  }

  appendLine(text: string = "\n"): IndentedStringBuilder {
    const indent = " ".repeat(this.spaces);
    const lines = text.split("\n");
    const indentedLines = lines.map((x) => indent + x.trimEnd());
    const indentedText = indentedLines.join("\n").trimEnd();
    this.builder.push(`${indentedText}\n`);
    return this;
  }

  indent(addSpaces: number = 0): IndentedStringBuilder {
    this.spaces = Math.max(0, this.spaces + addSpaces);
    return this;
  }

  dedent(removeSpaces: number = 0): IndentedStringBuilder {
    this.spaces = Math.max(0, this.spaces - removeSpaces);
    return this;
  }

  codeBlock(
    openingLine: string,
    spaces: number,
    fn: () => void,
    open: string = "{",
    close: string = "}"
  ): IndentedStringBuilder {
    if (openingLine) {
      this.append(openingLine);
      this.appendEnd(` ${open}`);
    } else {
      this.appendLine(open);
    }

    this.indent(spaces);
    fn();
    this.dedent(spaces);
    this.appendLine(close);
    return this;
  }

  toString(): string {
    return this.builder.join("");
  }
}


export type BaseType = "uint8" | "uint16" | "uint32" | "uint64" | "int8" | "int16" | "int32" | "int64" | "float32" | "float64" | "bool" | "string" | "guid" | "date";

export type Kind = "enum" | "struct" | "message" | "union" | "service" | "const";


export type DecoratorArgument = {
  type: BaseType;
  value: string;
};

export type Decorator = {
  identifier: string;
  arguments?: {
    [key: string]: DecoratorArgument;
  };
};

export type ArrayType = {
  depth: number;
  memberType: BaseType | "map";
  map?: MapType;
};

export type MapType = {
  keyType: BaseType;
  valueType: BaseType | "map" | "array";
  array?: ArrayType;
  map?: MapType;
};

export type Field<T extends BaseType | "array" | "map"> = {
  documentation?: string;
  decorators?: Decorator[];
  type: T;
  index?: number;
  array?: T extends "array" ? ArrayType : never;
  map?: T extends "map" ? MapType : never;
};

export type EnumMember = {
  documentation?: string;
  decorators?: Decorator[];
  value: string;
};

export type BaseDefinition<K extends Kind> = {
  kind: K;
  documentation?: string;
  decorators?: Decorator[];
  minimalEncodedSize: number;
  discriminatorInParent?: number;
  parent?: string;
};

export type EnumDefinition = BaseDefinition<"enum"> & {
  isBitFlags?: boolean;
  baseType?: BaseType;
  members: {
    [key: string]: EnumMember;
  };
};

export type StructDefinition = BaseDefinition<"struct"> & {
  mutable: boolean;
  isFixedSize: boolean;
  fields: {
    [key: string]: Field<BaseType | "array" | "map">;
  };
};

export type MessageDefinition = BaseDefinition<"message"> & {
  fields: {
    [key: string]: Field<BaseType | "array" | "map">;
  };
};

export type UnionDefinition = BaseDefinition<"union"> & {
  branches: {
    [key: string]: number;
  };
};

export type Method = {
  decorators?: Decorator[];
  documentation?: string;
  type: "Unary" | "DuplexStream" | "ClientStream" | "ServerStream";
  requestType: string;
  responseType: string;
  id: number;
};

export type ServiceDefinition = BaseDefinition<"service"> & {
  methods: {
    [key: string]: Method;
  };
};

export type ConstDefinition = BaseDefinition<"const"> & {
  type: BaseType;
  value: string;
};

export type Config = {
  alias: string;
  outFile: string;
  namespace: string;
  emitNotice: boolean;
  emitBinarySchema: boolean;
  services: "both" | "server" | "client";
  options: {
    [key: string]: string;
  };
};

export type CompilerContext = {
  definitions: {
    [key: string]: EnumDefinition | StructDefinition | MessageDefinition | UnionDefinition;
  };
  services: {
    [key: string]: ServiceDefinition;
  };
  constants: {
    [key: string]: ConstDefinition;
  };
  config: Config;
};
