
const int32 = new Int32Array(1);
const float32 = new Float32Array(int32.buffer);
const guidByteOrder = [3, 2, 1, 0, 5, 4, 7, 6, 8, 9, 10, 11, 12, 13, 14, 15];

/**
 * A hand-crafted byte buffer that is
 */
export class PierogiView {

    private data: Uint8Array;
    private index: number;
    length: number;

    constructor(data?: Uint8Array) {
        if (data && !(data instanceof Uint8Array)) {
            throw new Error("data is not a Uint8Array");
        }
        this.data = data || new Uint8Array(256);
        this.index = 0;
        this.length = data ? data.length : 0;
    }

    toArray(): Uint8Array {
        return this.data.subarray(0, this.length);
    }

    readByte(): number {
        if (this.index + 1 > this.data.length) {
            throw new Error("Index out of bounds");
        }
        return this.data[this.index++];
    }

    readFloat(): number {
        const index = this.index;
        const data = this.data;
        const length = data.length;

        // use a single byte to store zero
        if (index + 1 > length) {
            throw new Error("Index out of bounds");
        }
        const first = data[index];
        if (first === 0) {
            this.index = index + 1;
            return 0;
        }

        // 32-bit read
        if (index + 4 > length) {
            throw new Error("Index out of bounds");
        }
        let bits = first | (data[index + 1] << 8) | (data[index + 2] << 16) | (data[index + 3] << 24);
        this.index = index + 4;

        // move the exponent back into place
        bits = (bits << 23) | (bits >>> 9);

        // reinterpret as a floating-point number
        int32[0] = bits;
        return float32[0];
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

    private growBy(amount: number): void {
        if (this.length + amount > this.data.length) {
            const data = new Uint8Array(this.length + amount << 1);
            data.set(this.data);
            this.data = data;
        }
        this.length += amount;
    }

    readGuid(): string {
        const result = new Uint8Array(16);
        result[3] = this.readGuidByte();
        result[2] = this.readGuidByte();
        result[1] = this.readGuidByte();
        result[0] = this.readGuidByte();

        result[5] = this.readGuidByte();
        result[4] = this.readGuidByte();

        result[7] = this.readGuidByte();
        result[6] = this.readGuidByte();

        result[8] = this.readGuidByte();
        result[9] = this.readGuidByte();

        result[10] = this.readGuidByte();
        result[11] = this.readGuidByte();
        result[12] = this.readGuidByte();
        result[13] = this.readGuidByte();
        result[14] = this.readGuidByte();
        result[15] = this.readGuidByte();
        return String.fromCharCode(...result);
    }

    skip(amount: number) {
        this.index += amount;
    }

    readGuidByte(): number {
        let a = this.readByte();
        if (a === -1) throw new Error("Expected any character");
        if (!((a >= 0x30 && a <= 0x39) || (a >= 0x41 && a <= 0x46) || (a >= 0x61 && a <= 0x66)))
            throw new Error("Expected a hex number");
        let b = this.readByte();
        if (b === -1) throw new Error("Expected any character");
        if (!((b >= 0x30 && b <= 0x39) || (b >= 0x41 && b <= 0x46) || (b >= 0x61 && b <= 0x66)))
            throw new Error("Expected a hex number");

        if (a <= 0x39) {
            a -= 0x30;
        } else {
            if (a <= 0x46) {
                a -= (0x41 - 10);
            } else {
                a -= (0x61 - 10);
            }
        }

        if (b <= 0x39) {
            b -= 0x30;
        } else {
            if (b <= 0x46) {
                b -= (0x41 - 10);
            } else {
                b -= (0x61 - 10);
            }
        }
        return (a * 16 + b);
    }

    readBytes(): Uint8Array {
        const length = this.readUint();
        const start = this.index;
        const end = start + length;
        if (end > this.data.length) {
            throw new Error("Read array out of bounds");
        }
        this.index = end;
        const result = this.data.subarray(start, end);
        return result;
    }

    readInt(): number {
        const value = this.readUint() | 0;
        return value & 1 ? ~(value >>> 1) : value >>> 1;
    }

    readUint(): number {
        let value = 0;
        let shift = 0;
        let byte: number;
        do {
            byte = this.readByte();
            value |= (byte & 127) << shift;
            shift += 7;
        } while (byte & 128 && shift < 35);
        return value >>> 0;
    }

    writeByte(value: number): void {
        const index = this.length;
        this.growBy(1);
        this.data[index] = value;
    }

    writeGuid(value: string): void {

        const result = new Uint8Array(16);

    }
    /**
     * Writes a byte array to the view.
     * @todo optimize, why is this so slow?
     * @param value
     */
    writeBytes(value: Uint8Array): void {
        this.writeUint(value.length);
        const index = this.length;
        this.growBy(value.length);
        this.data.set(value, index);
    }

    writeFloat(value: number): void {
        const index = this.length;

        // Reinterpret as an integer
        float32[0] = value;
        let bits = int32[0];

        // Move the exponent to the first 8 bits
        bits = (bits >>> 23) | (bits << 9);

        // Optimization: use a single byte to store zero and denormals (check for an exponent of 0)
        if ((bits & 255) === 0) {
            this.writeByte(0);
            return;
        }

        // Endian-independent 32-bit write
        this.growBy(4);
        const data = this.data;
        data[index] = bits;
        data[index + 1] = bits >> 8;
        data[index + 2] = bits >> 16;
        data[index + 3] = bits >> 24;
    }

    writeUint(value: number): void {
        do {
            const byte = value & 127;
            value >>>= 7;
            this.writeByte(value ? byte | 128 : byte);
        } while (value);
    }

    writeEnum(value: any): void {
        var encoded = value as number;
        if (encoded === void 0) throw new Error("Couldn't convert enum value");
        this.writeUint(encoded);
    }

    writeInt(value: number): void {
        this.writeUint((value << 1) ^ (value >> 31));
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