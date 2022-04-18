import byteToHex from "./byte-to-hex";

const asciiToHex = Object.freeze([
  0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
  0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 2, 3,
  4, 5, 6, 7, 8, 9, 0, 0, 0, 0, 0, 0, 0, 10, 11, 12, 13, 14, 15, 0, 0, 0, 0, 0,
  0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 10, 11, 12, 13,
  14, 15, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
  0, 0,
]);
const emptyByteArray = Object.freeze(new Uint8Array(0));
const emptyString = Object.freeze("");
const ticksBetweenEpochs = 621355968000000000n;
const dateMask = 0x3fffffffffffffffn;

export class BebopView {
  private static textDecoder = new TextDecoder();
  private static writeBuffer: Uint8Array = new Uint8Array(256);
  private static writeBufferView: DataView = new DataView(
    BebopView.writeBuffer.buffer
  );
  private static instance: BebopView;

  public static getInstance(): BebopView {
    if (!BebopView.instance) {
      BebopView.instance = new BebopView();
    }
    return BebopView.instance;
  }

  minimumTextDecoderLength = 300;
  private buffer: Uint8Array;
  private view: DataView;
  index: number; // read pointer
  length: number; // write pointer

  private constructor() {
    this.buffer = BebopView.writeBuffer;
    this.view = BebopView.writeBufferView;
    this.index = 0;
    this.length = 0;
  }

  startReading(buffer: Uint8Array): void {
    this.buffer = buffer;
    this.view = new DataView(
      this.buffer.buffer,
      this.buffer.byteOffset,
      this.buffer.byteLength
    );
    this.index = 0;
    this.length = buffer.length;
  }

  startWriting(): void {
    this.buffer = BebopView.writeBuffer;
    this.view = BebopView.writeBufferView;
    this.index = 0;
    this.length = 0;
  }

  private guaranteeBufferLength(length: number): void {
    if (length > this.buffer.length) {
      const data = new Uint8Array(length << 1);
      data.set(this.buffer);
      this.buffer = data;
      this.view = new DataView(data.buffer);
    }
  }

  private growBy(amount: number): void {
    this.length += amount;
    this.guaranteeBufferLength(this.length);
  }

  skip(amount: number) {
    this.index += amount;
  }

  toArray(): Uint8Array {
    return this.buffer.subarray(0, this.length);
  }

  readByte(): number {
    return this.buffer[this.index++];
  }

  readUint16(): number {
    const result = this.view.getUint16(this.index, true);
    this.index += 2;
    return result;
  }

  readInt16(): number {
    const result = this.view.getInt16(this.index, true);
    this.index += 2;
    return result;
  }

  readUint32(): number {
    const result = this.view.getUint32(this.index, true);
    this.index += 4;
    return result;
  }

  readInt32(): number {
    const result = this.view.getInt32(this.index, true);
    this.index += 4;
    return result;
  }

  readUint64(): bigint {
    const result = this.view.getBigUint64(this.index, true);
    this.index += 8;
    return result;
  }

  readInt64(): bigint {
    const result = this.view.getBigInt64(this.index, true);
    this.index += 8;
    return result;
  }

  readFloat32(): number {
    const result = this.view.getFloat32(this.index, true);
    this.index += 4;
    return result;
  }

  readFloat64(): number {
    const result = this.view.getFloat64(this.index, true);
    this.index += 8;
    return result;
  }

  writeByte(value: number): void {
    const index = this.length;
    this.growBy(1);
    this.buffer[index] = value;
  }

  writeUint16(value: number): void {
    const index = this.length;
    this.growBy(2);
    this.view.setUint16(index, value, true);
  }

  writeInt16(value: number): void {
    const index = this.length;
    this.growBy(2);
    this.view.setInt16(index, value, true);
  }

  writeUint32(value: number): void {
    const index = this.length;
    this.growBy(4);
    this.view.setUint32(index, value, true);
  }

  writeInt32(value: number): void {
    const index = this.length;
    this.growBy(4);
    this.view.setInt32(index, value, true);
  }

  writeUint64(value: bigint): void {
    const index = this.length;
    this.growBy(8);
    this.view.setBigUint64(index, value, true);
  }

  writeInt64(value: bigint): void {
    const index = this.length;
    this.growBy(8);
    this.view.setBigInt64(index, value, true);
  }

  writeFloat32(value: number): void {
    const index = this.length;
    this.growBy(4);
    this.view.setFloat32(index, value, true);
  }

  writeFloat64(value: number): void {
    const index = this.length;
    this.growBy(8);
    this.view.setFloat64(index, value, true);
  }

  readBytes(): Uint8Array {
    const length = this.readUint32();
    if (length === 0) {
      return emptyByteArray;
    }
    const start = this.index,
      end = start + length;
    this.index = end;
    return this.buffer.subarray(start, end);
  }

  writeBytes(value: Uint8Array): void {
    const byteCount = value.length;
    this.writeUint32(byteCount);
    if (byteCount === 0) {
      return;
    }
    const index = this.length;
    this.growBy(byteCount);
    this.buffer.set(value, index);
  }

  /**
   * Reads a length-prefixed UTF-8-encoded string.
   */
  readString(): string {
    const lengthBytes = this.readUint32();
    // bail out early on an empty string
    if (lengthBytes === 0) {
      return emptyString;
    }
    if (lengthBytes >= this.minimumTextDecoderLength) {
      return BebopView.textDecoder.decode(
        this.buffer.subarray(this.index, (this.index += lengthBytes))
      );
    }

    const end = this.index + lengthBytes;
    let result = "";
    let codePoint: number;
    while (this.index < end) {
      // decode UTF-8
      const a = this.buffer[this.index++];
      if (a < 0xc0) {
        codePoint = a;
      } else {
        const b = this.buffer[this.index++];
        if (a < 0xe0) {
          codePoint = ((a & 0x1f) << 6) | (b & 0x3f);
        } else {
          const c = this.buffer[this.index++];
          if (a < 0xf0) {
            codePoint = ((a & 0x0f) << 12) | ((b & 0x3f) << 6) | (c & 0x3f);
          } else {
            const d = this.buffer[this.index++];
            codePoint =
              ((a & 0x07) << 18) |
              ((b & 0x3f) << 12) |
              ((c & 0x3f) << 6) |
              (d & 0x3f);
          }
        }
      }

      // encode UTF-16
      if (codePoint < 0x10000) {
        result += String.fromCharCode(codePoint);
      } else {
        codePoint -= 0x10000;
        result += String.fromCharCode(
          (codePoint >> 10) + 0xd800,
          (codePoint & ((1 << 10) - 1)) + 0xdc00
        );
      }
    }

    // Damage control, if the input is malformed UTF-8.
    this.index = end;

    return result;
  }

  /**
   * Writes a length-prefixed UTF-8-encoded string.
   */
  writeString(value: string): void {
    // The number of characters in the string
    const stringLength = value.length;
    // If the string is empty avoid unnecessary allocations by writing the zero length and returning.
    if (stringLength === 0) {
      this.writeUint32(0);
      return;
    }
    // value.length * 3 is an upper limit for the space taken up by the string:
    // https://developer.mozilla.org/en-US/docs/Web/API/TextEncoder/encodeInto#Buffer_Sizing
    // We add 4 for our length prefix.
    const maxBytes = 4 + stringLength * 3;

    // Reallocate if necessary, then write to this.length + 4.
    this.guaranteeBufferLength(this.length + maxBytes);

    // Start writing the string from here:
    let w = this.length + 4;
    const start = w;

    let codePoint: number;

    for (let i = 0; i < stringLength; i++) {
      // decode UTF-16
      const a = value.charCodeAt(i);
      if (i + 1 === stringLength || a < 0xd800 || a >= 0xdc00) {
        codePoint = a;
      } else {
        const b = value.charCodeAt(++i);
        codePoint = (a << 10) + b + (0x10000 - (0xd800 << 10) - 0xdc00);
      }

      // encode UTF-8
      if (codePoint < 0x80) {
        this.buffer[w++] = codePoint;
      } else {
        if (codePoint < 0x800) {
          this.buffer[w++] = ((codePoint >> 6) & 0x1f) | 0xc0;
        } else {
          if (codePoint < 0x10000) {
            this.buffer[w++] = ((codePoint >> 12) & 0x0f) | 0xe0;
          } else {
            this.buffer[w++] = ((codePoint >> 18) & 0x07) | 0xf0;
            this.buffer[w++] = ((codePoint >> 12) & 0x3f) | 0x80;
          }
          this.buffer[w++] = ((codePoint >> 6) & 0x3f) | 0x80;
        }
        this.buffer[w++] = (codePoint & 0x3f) | 0x80;
      }
    }

    // Count how many bytes we wrote.
    const written = w - start;

    // Write the length prefix, then skip over it and the written string.
    this.view.setUint32(this.length, written, true);
    this.length += 4 + written;
  }

  readGuid(): string {
    // Order: 3 2 1 0 - 5 4 - 7 6 - 8 9 - a b c d e f
    const b = byteToHex,
      a = this.buffer,
      i = this.index,
      d = "-";
    let s = b[a[i + 3]];
    s += b[a[i + 2]];
    s += b[a[i + 1]];
    s += b[a[i]];
    s += d;
    s += b[a[i + 5]];
    s += b[a[i + 4]];
    s += d;
    s += b[a[i + 7]];
    s += b[a[i + 6]];
    s += d;
    s += b[a[i + 8]];
    s += b[a[i + 9]];
    s += d;
    s += b[a[i + 10]];
    s += b[a[i + 11]];
    s += b[a[i + 12]];
    s += b[a[i + 13]];
    s += b[a[i + 14]];
    s += b[a[i + 15]];
    this.index += 16;
    return s;
  }

  writeGuid(value: string): void {
    const v = this.view,
      i = this.length;
    this.growBy(16);
    let p = 0,
      a = 0;
    a = (a << 4) | asciiToHex[value.charCodeAt(p++)];
    a = (a << 4) | asciiToHex[value.charCodeAt(p++)];
    a = (a << 4) | asciiToHex[value.charCodeAt(p++)];
    a = (a << 4) | asciiToHex[value.charCodeAt(p++)];
    a = (a << 4) | asciiToHex[value.charCodeAt(p++)];
    a = (a << 4) | asciiToHex[value.charCodeAt(p++)];
    a = (a << 4) | asciiToHex[value.charCodeAt(p++)];
    a = (a << 4) | asciiToHex[value.charCodeAt(p++)];
    p += (value.charCodeAt(p) === 45) as unknown as number;
    v.setUint32(i, a, true);
    a = (a << 4) | asciiToHex[value.charCodeAt(p++)];
    a = (a << 4) | asciiToHex[value.charCodeAt(p++)];
    a = (a << 4) | asciiToHex[value.charCodeAt(p++)];
    a = (a << 4) | asciiToHex[value.charCodeAt(p++)];
    p += (value.charCodeAt(p) === 45) as unknown as number;
    v.setUint16(i + 4, a, true);
    a = (a << 4) | asciiToHex[value.charCodeAt(p++)];
    a = (a << 4) | asciiToHex[value.charCodeAt(p++)];
    a = (a << 4) | asciiToHex[value.charCodeAt(p++)];
    a = (a << 4) | asciiToHex[value.charCodeAt(p++)];
    p += (value.charCodeAt(p) === 45) as unknown as number;
    v.setUint16(i + 6, a, true);
    a = (a << 4) | asciiToHex[value.charCodeAt(p++)];
    a = (a << 4) | asciiToHex[value.charCodeAt(p++)];
    a = (a << 4) | asciiToHex[value.charCodeAt(p++)];
    a = (a << 4) | asciiToHex[value.charCodeAt(p++)];
    p += (value.charCodeAt(p) === 45) as unknown as number;
    a = (a << 4) | asciiToHex[value.charCodeAt(p++)];
    a = (a << 4) | asciiToHex[value.charCodeAt(p++)];
    a = (a << 4) | asciiToHex[value.charCodeAt(p++)];
    a = (a << 4) | asciiToHex[value.charCodeAt(p++)];
    v.setUint32(i + 8, a, false);
    a = (a << 4) | asciiToHex[value.charCodeAt(p++)];
    a = (a << 4) | asciiToHex[value.charCodeAt(p++)];
    a = (a << 4) | asciiToHex[value.charCodeAt(p++)];
    a = (a << 4) | asciiToHex[value.charCodeAt(p++)];
    a = (a << 4) | asciiToHex[value.charCodeAt(p++)];
    a = (a << 4) | asciiToHex[value.charCodeAt(p++)];
    a = (a << 4) | asciiToHex[value.charCodeAt(p++)];
    a = (a << 4) | asciiToHex[value.charCodeAt(p++)];
    v.setUint32(i + 12, a, false);
  }

  readDate(): Date {
    const ticks = this.readUint64() & dateMask;
    const ms = (ticks - ticksBetweenEpochs) / 10000n;
    return new Date(Number(ms));
  }

  writeDate(date: Date) {
    const ms = BigInt(date.getTime());
    const ticks = ms * 10000n + ticksBetweenEpochs;
    this.writeUint64(ticks & dateMask);
  }

  /**
   * Reserve some space to write a message's length prefix, and return its index.
   * The length is stored as a little-endian fixed-width unsigned 32-bit integer, so 4 bytes are reserved.
   */
  reserveMessageLength(): number {
    const i = this.length;
    this.growBy(4);
    return i;
  }

  /**
   * Fill in a message's length prefix.
   */
  fillMessageLength(position: number, messageLength: number): void {
    this.view.setUint32(position, messageLength, true);
  }

  /**
   * Read out a message's length prefix.
   */
  readMessageLength(): number {
    const result = this.view.getUint32(this.index, true);
    this.index += 4;
    return result;
  }
}
