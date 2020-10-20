
const hexDigits = "0123456789abcdef";
const asciiToHex = [
    0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,
    0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,
    0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,
    0,  1,  2,  3,  4,  5,  6,  7,  8,  9,  0,  0,  0,  0,  0,  0,
    0, 10, 11, 12, 13, 14, 15,  0,  0,  0,  0,  0,  0,  0,  0,  0,
    0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,
    0, 10, 11, 12, 13, 14, 15,  0,  0,  0,  0,  0,  0,  0,  0,  0,
    0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0
];

export class PierogiView {
    private buffer: Uint8Array;
    private view: DataView;
    private index: number;
    private byteToHex: string[] = []; // A lookup table: ['00', '01', ..., 'ff']
    length: number;

    constructor(data?: Uint8Array) {
        if (data && !(data instanceof Uint8Array)) {
            throw new Error("data is not a Uint8Array");
        }
        this.buffer = data || new Uint8Array(256);
        this.view = new DataView(this.buffer.buffer);
        this.index = 0;
        this.length = data ? data.length : 0;
        for (const x of hexDigits) {
            for (const y of hexDigits) {
                this.byteToHex.push(x + y);
            }
        }
    }

    private growBy(amount: number): void {
        if (this.length + amount > this.buffer.length) {
            const data = new Uint8Array(this.length + amount << 1);
            data.set(this.buffer);
            this.buffer = data;
            this.view = new DataView(data.buffer);
        }
        this.length += amount;
    }

    rewind(): void {
        this.index = 0;
        this.length = 0;
    }

    toArray(): Uint8Array {
        return this.buffer.subarray(0, this.length);
    }

    readByte(): number {
        return this.buffer[this.index++];
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

    readString(): string {
        let result = "";

        while (true) {
            let codePoint: number;

            // decode UTF-8
            const a = this.readByte();
            if (a < 0xC0) {
                codePoint = a;
            } else {
                const b = this.readByte();
                if (a < 0xE0) {
                    codePoint = ((a & 0x1F) << 6) | (b & 0x3F);
                } else {
                    const c = this.readByte();
                    if (a < 0xF0) {
                        codePoint = ((a & 0x0F) << 12) | ((b & 0x3F) << 6) | (c & 0x3F);
                    } else {
                        const d = this.readByte();
                        codePoint = ((a & 0x07) << 18) | ((b & 0x3F) << 12) | ((c & 0x3F) << 6) | (d & 0x3F);
                    }
                }
            }

            // strings are null-terminated
            if (codePoint === 0) {
                break;
            }

            // encode UTF-16
            if (codePoint < 0x10000) {
                result += String.fromCharCode(codePoint);
            } else {
                codePoint -= 0x10000;
                result += String.fromCharCode((codePoint >> 10) + 0xD800, (codePoint & ((1 << 10) - 1)) + 0xDC00);
            }
        }

        return result;
    }

    readGuid(): string {
        if (this.index + 16 > this.buffer.length) {
            throw new Error("Index out of bounds");
        }

        // Order: 3 2 1 0 - 5 4 - 7 6 - 8 9 - a b c d e f
        const b = this.byteToHex, a = this.buffer, i = this.index, d = '-';
        var s = b[a[i + 3]];
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

    skip(amount: number) {
        this.index += amount;
    }

    readBytes(): Uint8Array {
        const length = this.readUint();
        const start = this.index;
        const end = start + length;
        if (end > this.buffer.length) {
            throw new Error("Read array out of bounds");
        }
        this.index = end;
        const result = this.buffer.subarray(start, end);
        return result;
    }

    readFloat32s(): Float32Array {
        const length = this.readUint();
        const result = new Float32Array(this.buffer, this.index, length);
        this.index += length * 4;
        return result;
    }

    readFloat64s(): Float64Array {
        const length = this.readUint();
        const result = new Float64Array(this.buffer, this.index, length);
        this.index += length * 8;
        return result;
    }

    readInt(): number {
        const value = this.readUint() | 0;
        return value & 1 ? ~(value >> 1) : value >> 1;
    }

    readUint(): number {
        let value = 0;
        let shift = 0;
        let byte: number;
        do {
            byte = this.readByte();
            value |= (byte & 127) << shift;
            shift += 7;
        } while (byte & 128);
        return value >> 0;
    }

    readBigInt(): bigint {
        const value = this.readBigUint();
        const half = value >> BigInt(1);
        return value & BigInt(1) ? ~half : half;
    }

    readBigUint(): bigint {
        let value = BigInt(0);
        let shift = BigInt(0);
        let byte: number;
        do {
            byte = this.readByte();
            value |= BigInt(byte & 127) << shift;
            shift += BigInt(7);
        } while (byte & 128);
        return value;
    }

    writeByte(value: number): void {
        const index = this.length;
        this.growBy(1);
        this.buffer[index] = value;
    }

    writeGuid(value: string): void {
        const v = this.view, i = this.length;
        this.growBy(16);
        var p = 0, a = 0;
        a = a<<4 | asciiToHex[value.charCodeAt(p++)];
        a = a<<4 | asciiToHex[value.charCodeAt(p++)];
        a = a<<4 | asciiToHex[value.charCodeAt(p++)];
        a = a<<4 | asciiToHex[value.charCodeAt(p++)];
        a = a<<4 | asciiToHex[value.charCodeAt(p++)];
        a = a<<4 | asciiToHex[value.charCodeAt(p++)];
        a = a<<4 | asciiToHex[value.charCodeAt(p++)];
        a = a<<4 | asciiToHex[value.charCodeAt(p++)];
        p += (value.charCodeAt(p) === 45) as any;
        v.setUint32(i, a, true);
        a = a<<4 | asciiToHex[value.charCodeAt(p++)];
        a = a<<4 | asciiToHex[value.charCodeAt(p++)];
        a = a<<4 | asciiToHex[value.charCodeAt(p++)];
        a = a<<4 | asciiToHex[value.charCodeAt(p++)];
        p += (value.charCodeAt(p) === 45) as any;
        v.setUint16(i + 4, a, true);
        a = a<<4 | asciiToHex[value.charCodeAt(p++)];
        a = a<<4 | asciiToHex[value.charCodeAt(p++)];
        a = a<<4 | asciiToHex[value.charCodeAt(p++)];
        a = a<<4 | asciiToHex[value.charCodeAt(p++)];
        p += (value.charCodeAt(p) === 45) as any;
        v.setUint16(i + 6, a, true);
        a = a<<4 | asciiToHex[value.charCodeAt(p++)];
        a = a<<4 | asciiToHex[value.charCodeAt(p++)];
        a = a<<4 | asciiToHex[value.charCodeAt(p++)];
        a = a<<4 | asciiToHex[value.charCodeAt(p++)];
        p += (value.charCodeAt(p) === 45) as any;
        a = a<<4 | asciiToHex[value.charCodeAt(p++)];
        a = a<<4 | asciiToHex[value.charCodeAt(p++)];
        a = a<<4 | asciiToHex[value.charCodeAt(p++)];
        a = a<<4 | asciiToHex[value.charCodeAt(p++)];
        v.setUint32(i + 8, a, false);
        a = a<<4 | asciiToHex[value.charCodeAt(p++)];
        a = a<<4 | asciiToHex[value.charCodeAt(p++)];
        a = a<<4 | asciiToHex[value.charCodeAt(p++)];
        a = a<<4 | asciiToHex[value.charCodeAt(p++)];
        a = a<<4 | asciiToHex[value.charCodeAt(p++)];
        a = a<<4 | asciiToHex[value.charCodeAt(p++)];
        a = a<<4 | asciiToHex[value.charCodeAt(p++)];
        a = a<<4 | asciiToHex[value.charCodeAt(p++)];
        v.setUint32(i + 12, a, false);
    }

    /**
     * Writes a byte array to the view.
     */
    writeBytes(value: Uint8Array): void {
        this.writeUint(value.length);
        const index = this.length;
        this.growBy(value.length);
        this.buffer.set(value, index);
    }

    writeFloat32s(value: Float32Array): void {
        this.writeUint(value.length);
        const index = this.length;
        this.growBy(value.length * 4);
        this.buffer.set(value, index);
    }

    writeFloat64s(value: Float64Array): void {
        this.writeUint(value.length);
        const index = this.length;
        this.growBy(value.length * 8);
        this.buffer.set(value, index);
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

    writeUint(value: number, maxValue?: number): void {
        if (maxValue !== undefined && value > maxValue) {
            throw new Error(`Value ${value} is greater than maximum allowed value ${maxValue}`);
        }
        do {
            const byte = value & 127;
            value >>= 7;
            this.writeByte(value ? byte | 128 : byte);
        } while (value);
    }

    writeBigUint(value: bigint): void {
        do {
            const byte = Number(value & BigInt(127));
            value >>= BigInt(7);
            this.writeByte(value ? byte | 128 : byte);
        } while (value);
    }

    writeEnum(value: any): void {
        var encoded = value as number;
        if (encoded === void 0) throw new Error("Couldn't convert enum value");
        this.writeUint(encoded, 0xFFFFFFFF);
    }

    writeInt(value: number, minValue: number, maxValue: number): void {
        if (value < minValue) throw new Error(`Value ${value} is less than minimum allowed value ${minValue}`);
        if (value > maxValue) throw new Error(`Value ${value} is greater than maximum allowed value ${maxValue}`);
        this.writeUint((value << 1) ^ (value >> 31));
    }

    writeBigInt(value: bigint): void {
        this.writeBigUint(value >= 0 ? BigInt(2) * value : ~(BigInt(2) * value));
    }

    /**
     * Writes a null-terminated string using UTF-8 encoding.
     * This method first writes the length of the string as
     * a variable-length unsigned integer, and then writes that many characters to the buffer.
     * @param value
     */
    writeString(value: string): void {
        let codePoint: number;

        for (let i = 0; i < value.length; i++) {
            // decode UTF-16
            const a = value.charCodeAt(i);
            if (i + 1 === value.length || a < 0xD800 || a >= 0xDC00) {
                codePoint = a;
            } else {
                const b = value.charCodeAt(++i);
                codePoint = (a << 10) + b + (0x10000 - (0xD800 << 10) - 0xDC00);
            }

            // strings are null-terminated, so a string cannot contain a null character.
            if (codePoint === 0) {
                throw new Error("Cannot encode a string containing the null character");
            }

            // encode UTF-8
            if (codePoint < 0x80) {
                this.writeByte(codePoint);
            } else {
                if (codePoint < 0x800) {
                    this.writeByte(((codePoint >> 6) & 0x1F) | 0xC0);
                } else {
                    if (codePoint < 0x10000) {
                        this.writeByte(((codePoint >> 12) & 0x0F) | 0xE0);
                    } else {
                        this.writeByte(((codePoint >> 18) & 0x07) | 0xF0);
                        this.writeByte(((codePoint >> 12) & 0x3F) | 0x80);
                    }
                    this.writeByte(((codePoint >> 6) & 0x3F) | 0x80);
                }
                this.writeByte((codePoint & 0x3F) | 0x80);
            }
        }

        // write the null terminator
        this.writeByte(0);
    }
}
