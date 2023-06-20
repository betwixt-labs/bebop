
const hexDigits = "0123456789abcdef";
const asciiToHex = [
    0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
    0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
    0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
    0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 0, 0, 0, 0, 0, 0,
    0, 10, 11, 12, 13, 14, 15, 0, 0, 0, 0, 0, 0, 0, 0, 0,
    0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
    0, 10, 11, 12, 13, 14, 15, 0, 0, 0, 0, 0, 0, 0, 0, 0,
    0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0
];

const guidDelimiter = "-";
const ticksBetweenEpochs = 621355968000000000n;
const dateMask = 0x3fffffffffffffffn;
const emptyByteArray = new Uint8Array(0);
const emptyString = "";
const byteToHex: string[] = []; // A lookup table: ['00', '01', ..., 'ff']
for (const x of hexDigits) {
    for (const y of hexDigits) {
        byteToHex.push(x + y);
    }
}

export class BebopRuntimeError extends Error {
    constructor(message: string) {
        super(message);
        this.name = "BebopRuntimeError";
    }
}


/**
 * Represents a globally unique identifier (GUID).
 */
export class Guid {
   public static readonly empty = new Guid("00000000-0000-0000-0000-000000000000");
    /**
       * Constructs a new Guid object with the specified value.
       * @param value The value of the GUID.
       */
    private constructor(private readonly value: string) { }
    
    /**
     * Gets the string value of the Guid.
     * @returns The string representation of the Guid.
     */
    public toString(): string {
        return this.value;
    }

   /**
     * Checks if the Guid is empty.
     * @returns true if the Guid is empty, false otherwise.
     */
    public isEmpty(): boolean {
       return this.value === Guid.empty.value;
    }

    /**
     * Checks if a value is a Guid.
     * @param value The value to be checked.
     * @returns true if the value is a Guid, false otherwise.
     */
    public static isGuid(value: any): value is Guid {
        return value instanceof Guid;
    }

    /**
    * Parses a string into a Guid.
    * @param value The string to be parsed.
    * @returns A new Guid that represents the parsed value.
    * @throws {BebopRuntimeError} If the input string is not a valid Guid.
    */
    public static parseGuid(value: string): Guid {
        let cleanedInput = '';
        let count = 0;

        // Iterate through each character in the input
        for (let i = 0; i < value.length; i++) {
            let ch = value[i].toLowerCase();
            if (hexDigits.indexOf(ch) !== -1) {
                // If the character is a hexadecimal digit, add it to cleanedInput
                cleanedInput += ch;
                count++;
            } else if (ch !== '-') {
                // If the character is not a hexadecimal digit or a hyphen, it's invalid
                throw new BebopRuntimeError(`Invalid GUID: ${value}`)
            }
        }
        // If the count is not 32, the input is not a valid GUID
        if (count !== 32) {
            throw new BebopRuntimeError(`Invalid GUID: ${value}`)
        }
        // Insert hyphens to make it a 8-4-4-4-12 character pattern
        const guidString =
            cleanedInput.slice(0, 8) + '-' +
            cleanedInput.slice(8, 12) + '-' +
            cleanedInput.slice(12, 16) + '-' +
            cleanedInput.slice(16, 20) + '-' +
            cleanedInput.slice(20);
        // Construct a new Guid object with the generated string and return it
        return new Guid(guidString);
    }

    /**
    * Creates a new Guid.
    * @returns A new Guid.
    */
    public static newGuid(): Guid {
        let guid = "";
        // Obtain a single timestamp to help seed randomness
        const now = Date.now();

        // Iterate through the 36 characters of a UUID
        for (let i = 0; i < 36; i++) {
            // Insert hyphens at the appropriate indices (8, 13, 18, 23)
            if (i === 8 || i === 13 || i === 18 || i === 23) {
                guid += "-";
            }
            // According to the UUID v4 spec, the 14th character should be '4'
            else if (i === 14) {
                guid += "4";
            }
            // According to the UUID v4 spec, the 19th character should be one of '8', '9', 'a', or 'b'.
            // Here we're using 'a' or 'b' to simplify the code
            else if (i === 19) {
                guid += Math.random() > 0.5 ? "a" : "b";
            }
            // Generate the rest of the UUID using random hexadecimal digits
            else {
                // Add the current time to the random number to seed it, then modulo by 16 to get a number between 0 and 15
                // Use bitwise OR 0 to round the result down to an integer, and get the hexadecimal digit from the lookup table
                guid += hexDigits[(Math.random() * 16 + now) % 16 | 0];
            }
        }
        // Construct a new Guid object with the generated string and return it
        return new Guid(guid);
    }

    /**
    * Checks if the Guid is equal to another Guid.
    * @param other The other Guid to be compared with.
    * @returns true if the Guids are equal, false otherwise.
    */
    public equals(other: Guid): boolean {
        // Check if both GUIDs are the same instance
        if (this === other) {
            return true;
        }

        // Check if the other object is a GUID
        if (!(other instanceof Guid)) {
            return false;
        }

        // Compare the hexadecimal representations of both GUIDs
        for (let i = 0; i < this.value.length; i++) {
            if (this.value[i] !== other.value[i]) {
                return false;
            }
        }
        // All hexadecimal digits are equal, so the GUIDs are equal
        return true;
    }

    /**
    * Writes the Guid to a DataView.
    * @param view The DataView to write to.
    * @param length The position to start writing at.
    */
    public writeToView(view: DataView, length: number): void {
        var p = 0, a = 0;
        a = (a << 4) | asciiToHex[this.value.charCodeAt(p++)];
        a = (a << 4) | asciiToHex[this.value.charCodeAt(p++)];
        a = (a << 4) | asciiToHex[this.value.charCodeAt(p++)];
        a = (a << 4) | asciiToHex[this.value.charCodeAt(p++)];
        a = (a << 4) | asciiToHex[this.value.charCodeAt(p++)];
        a = (a << 4) | asciiToHex[this.value.charCodeAt(p++)];
        a = (a << 4) | asciiToHex[this.value.charCodeAt(p++)];
        a = (a << 4) | asciiToHex[this.value.charCodeAt(p++)];
        p += (this.value.charCodeAt(p) === 45) as any;
        view.setUint32(length, a, true);
        a = (a << 4) | asciiToHex[this.value.charCodeAt(p++)];
        a = (a << 4) | asciiToHex[this.value.charCodeAt(p++)];
        a = (a << 4) | asciiToHex[this.value.charCodeAt(p++)];
        a = (a << 4) | asciiToHex[this.value.charCodeAt(p++)];
        p += (this.value.charCodeAt(p) === 45) as any;
        view.setUint16(length + 4, a, true);
        a = (a << 4) | asciiToHex[this.value.charCodeAt(p++)];
        a = (a << 4) | asciiToHex[this.value.charCodeAt(p++)];
        a = (a << 4) | asciiToHex[this.value.charCodeAt(p++)];
        a = (a << 4) | asciiToHex[this.value.charCodeAt(p++)];
        p += (this.value.charCodeAt(p) === 45) as any;
        view.setUint16(length + 6, a, true);
        a = (a << 4) | asciiToHex[this.value.charCodeAt(p++)];
        a = (a << 4) | asciiToHex[this.value.charCodeAt(p++)];
        a = (a << 4) | asciiToHex[this.value.charCodeAt(p++)];
        a = (a << 4) | asciiToHex[this.value.charCodeAt(p++)];
        p += (this.value.charCodeAt(p) === 45) as any;
        a = (a << 4) | asciiToHex[this.value.charCodeAt(p++)];
        a = (a << 4) | asciiToHex[this.value.charCodeAt(p++)];
        a = (a << 4) | asciiToHex[this.value.charCodeAt(p++)];
        a = (a << 4) | asciiToHex[this.value.charCodeAt(p++)];
        view.setUint32(length + 8, a, false);
        a = (a << 4) | asciiToHex[this.value.charCodeAt(p++)];
        a = (a << 4) | asciiToHex[this.value.charCodeAt(p++)];
        a = (a << 4) | asciiToHex[this.value.charCodeAt(p++)];
        a = (a << 4) | asciiToHex[this.value.charCodeAt(p++)];
        a = (a << 4) | asciiToHex[this.value.charCodeAt(p++)];
        a = (a << 4) | asciiToHex[this.value.charCodeAt(p++)];
        a = (a << 4) | asciiToHex[this.value.charCodeAt(p++)];
        a = (a << 4) | asciiToHex[this.value.charCodeAt(p++)];
        view.setUint32(length + 12, a, false);
    }

    /**
    * Creates a Guid from a byte array.
    * @param buffer The byte array to create the Guid from.
    * @param index The position in the array to start reading from.
    * @returns A new Guid that represents the byte array.
    */
    public static fromBytes(buffer: Uint8Array, index: number): Guid {
        // Order: 3 2 1 0 - 5 4 - 7 6 - 8 9 - a b c d e f
        var s = byteToHex[buffer[index + 3]];
        s += byteToHex[buffer[index + 2]];
        s += byteToHex[buffer[index + 1]];
        s += byteToHex[buffer[index]];
        s += guidDelimiter;
        s += byteToHex[buffer[index + 5]];
        s += byteToHex[buffer[index + 4]];
        s += guidDelimiter;
        s += byteToHex[buffer[index + 7]];
        s += byteToHex[buffer[index + 6]];
        s += guidDelimiter;
        s += byteToHex[buffer[index + 8]];
        s += byteToHex[buffer[index + 9]];
        s += guidDelimiter;
        s += byteToHex[buffer[index + 10]];
        s += byteToHex[buffer[index + 11]];
        s += byteToHex[buffer[index + 12]];
        s += byteToHex[buffer[index + 13]];
        s += byteToHex[buffer[index + 14]];
        s += byteToHex[buffer[index + 15]];
        return new Guid(s);
    }
}


/**
 * Represents a wrapper around the `Map` class with support for using `Guid` instances as keys.
 *
 * This class is designed to provide a 1:1 mapping between `Guid` instances and values, allowing `Guid` instances to be used as keys in the map.
 * The class handles converting `Guid` instances to their string representation for key storage and retrieval.
 * @remarks this is required because Javascript lacks true reference equality. Thus two `Guid` instances with the same value are not equal.
 */
export class GuidMap<TValue> {
    private readonly map: Map<string, TValue>;

    /**
     * Creates a new GuidMap instance.
     * @param entries - An optional array or iterable containing key-value pairs to initialize the map.
     */
    constructor(
        entries?:
            | readonly (readonly [Guid, TValue])[]
            | null
            | Iterable<readonly [Guid, TValue]>
    ) {
        if (entries instanceof Map) {
            this.map = new Map<string, TValue>(
                entries as unknown as Iterable<[string, TValue]>
            );
        } else if (entries && typeof entries[Symbol.iterator] === "function") {
            this.map = new Map<string, TValue>(
                [...entries].map(([key, value]) => [key.toString(), value])
            );
        } else {
            this.map = new Map<string, TValue>();
        }
    }

    /**
     * Sets the value associated with the specified `Guid` key in the map.
     * @param key The `Guid` key.
     * @param value The value to be set.
     * @returns The updated `GuidMap` instance.
     */
    set(key: Guid, value: TValue): this {
        this.map.set(key.toString(), value);
        return this;
    }

    /**
     * Retrieves the value associated with the specified `Guid` key from the map.
     * @param key The `Guid` key.
     * @returns The associated value, or `undefined` if the key is not found.
     */
    get(key: Guid): TValue | undefined {
        return this.map.get(key.toString());
    }

    /**
     * Deletes the value associated with the specified `Guid` key from the map.
     * @param key The `Guid` key.
     * @returns `true` if the key was found and deleted, or `false` otherwise.
     */
    delete(key: Guid): boolean {
        return this.map.delete(key.toString());
    }

    /**
     * Checks if the map contains the specified `Guid` key.
     * @param key The `Guid` key.
     * @returns `true` if the key is found, or `false` otherwise.
     */
    has(key: Guid): boolean {
        return this.map.has(key.toString());
    }

    /**
     * Removes all entries from the map.
     */
    clear(): void {
        this.map.clear();
    }

    /**
     * Returns the number of entries in the map.
     * @returns The number of entries in the map.
     */
    get size(): number {
        return this.map.size;
    }

    /**
     * Executes the provided callback function once for each key-value pair in the map.
     * @param callbackFn The callback function to execute.
     */
    forEach(
        callbackFn: (value: TValue, key: Guid, map: GuidMap<TValue>) => void
    ): void {
        this.map.forEach((value, keyString) => {
            callbackFn(value, Guid.parseGuid(keyString), this);
        });
    }

    /**
     * Returns an iterator that yields key-value pairs in the map.
     * @returns An iterator for key-value pairs in the map.
     */
    *entries(): Generator<[Guid, TValue]> {
        for (const [keyString, value] of this.map.entries()) {
            yield [Guid.parseGuid(keyString), value];
        }
    }

    /**
     * Returns an iterator that yields the keys of the map.
     * @returns An iterator for the keys of the map.
     */
    *keys(): Generator<Guid> {
        for (const keyString of this.map.keys()) {
            yield Guid.parseGuid(keyString);
        }
    }

    /**
     * Returns an iterator that yields the values in the map.
     * @returns An iterator for the values in the map.
     */
    *values(): Generator<TValue> {
        yield* this.map.values() as Generator<TValue>;
    }

    /**
     * Returns an iterator that yields key-value pairs in the map.
     * This method is invoked when using the spread operator or destructuring the map.
     * @returns An iterator for key-value pairs in the map.
     */
    [Symbol.iterator](): Generator<[Guid, TValue]> {
        return this.entries();
    }

    /**
     * The constructor function used to create derived objects.
     */
    get [Symbol.species](): typeof GuidMap {
        return GuidMap;
    }
}

/**
 * An interface which all generated Bebop interfaces implement.
 * @note this interface is not currently used by the runtime; it is reserved for future use.
 */
export interface BebopRecord {

}
export class BebopView {
    private static textDecoder: TextDecoder;
    private static writeBuffer: Uint8Array = new Uint8Array(256);
    private static writeBufferView: DataView = new DataView(BebopView.writeBuffer.buffer);
    private static instance: BebopView;
    public static getInstance(): BebopView {
        if (!BebopView.instance) {
            BebopView.instance = new BebopView();
        }
        return BebopView.instance;
    }

    minimumTextDecoderLength: number = 300;
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
        this.view = new DataView(this.buffer.buffer, this.buffer.byteOffset, this.buffer.byteLength);
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

    readByte(): number { return this.buffer[this.index++]; }
    readUint16(): number { const result = this.view.getUint16(this.index, true); this.index += 2; return result; }
    readInt16(): number { const result = this.view.getInt16(this.index, true); this.index += 2; return result; }
    readUint32(): number { const result = this.view.getUint32(this.index, true); this.index += 4; return result; }
    readInt32(): number { const result = this.view.getInt32(this.index, true); this.index += 4; return result; }
    readUint64(): bigint { const result = this.view.getBigUint64(this.index, true); this.index += 8; return result; }
    readInt64(): bigint { const result = this.view.getBigInt64(this.index, true); this.index += 8; return result; }
    readFloat32(): number { const result = this.view.getFloat32(this.index, true); this.index += 4; return result; }
    readFloat64(): number { const result = this.view.getFloat64(this.index, true); this.index += 8; return result; }

    writeByte(value: number): void { const index = this.length; this.growBy(1); this.buffer[index] = value; }
    writeUint16(value: number): void { const index = this.length; this.growBy(2); this.view.setUint16(index, value, true); }
    writeInt16(value: number): void { const index = this.length; this.growBy(2); this.view.setInt16(index, value, true); }
    writeUint32(value: number): void { const index = this.length; this.growBy(4); this.view.setUint32(index, value, true); }
    writeInt32(value: number): void { const index = this.length; this.growBy(4); this.view.setInt32(index, value, true); }
    writeUint64(value: bigint): void { const index = this.length; this.growBy(8); this.view.setBigUint64(index, value, true); }
    writeInt64(value: bigint): void { const index = this.length; this.growBy(8); this.view.setBigInt64(index, value, true); }
    writeFloat32(value: number): void { const index = this.length; this.growBy(4); this.view.setFloat32(index, value, true); }
    writeFloat64(value: number): void { const index = this.length; this.growBy(8); this.view.setFloat64(index, value, true); }

    readBytes(): Uint8Array {
        const length = this.readUint32();
        if (length === 0) {
            return emptyByteArray;
        }
        const start = this.index, end = start + length;
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
            if (typeof require !== 'undefined') {
                if (typeof TextDecoder === 'undefined') {
                    throw new BebopRuntimeError("TextDecoder is not defined on 'global'. Please include a polyfill.");
                }
            }
            if (BebopView.textDecoder === undefined) {
                BebopView.textDecoder = new TextDecoder();
            }
            return BebopView.textDecoder.decode(this.buffer.subarray(this.index, this.index += lengthBytes));
        }

        const end = this.index + lengthBytes;
        let result = "";
        let codePoint: number;
        while (this.index < end) {
            // decode UTF-8
            const a = this.buffer[this.index++];
            if (a < 0xC0) {
                codePoint = a;
            } else {
                const b = this.buffer[this.index++];
                if (a < 0xE0) {
                    codePoint = ((a & 0x1F) << 6) | (b & 0x3F);
                } else {
                    const c = this.buffer[this.index++];
                    if (a < 0xF0) {
                        codePoint = ((a & 0x0F) << 12) | ((b & 0x3F) << 6) | (c & 0x3F);
                    } else {
                        const d = this.buffer[this.index++];
                        codePoint = ((a & 0x07) << 18) | ((b & 0x3F) << 12) | ((c & 0x3F) << 6) | (d & 0x3F);
                    }
                }
            }

            // encode UTF-16
            if (codePoint < 0x10000) {
                result += String.fromCharCode(codePoint);
            } else {
                codePoint -= 0x10000;
                result += String.fromCharCode((codePoint >> 10) + 0xD800, (codePoint & ((1 << 10) - 1)) + 0xDC00);
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
            if (i + 1 === stringLength || a < 0xD800 || a >= 0xDC00) {
                codePoint = a;
            } else {
                const b = value.charCodeAt(++i);
                codePoint = (a << 10) + b + (0x10000 - (0xD800 << 10) - 0xDC00);
            }

            // encode UTF-8
            if (codePoint < 0x80) {
                this.buffer[w++] = codePoint;
            } else {
                if (codePoint < 0x800) {
                    this.buffer[w++] = ((codePoint >> 6) & 0x1F) | 0xC0;
                } else {
                    if (codePoint < 0x10000) {
                        this.buffer[w++] = ((codePoint >> 12) & 0x0F) | 0xE0;
                    } else {
                        this.buffer[w++] = ((codePoint >> 18) & 0x07) | 0xF0;
                        this.buffer[w++] = ((codePoint >> 12) & 0x3F) | 0x80;
                    }
                    this.buffer[w++] = ((codePoint >> 6) & 0x3F) | 0x80;
                }
                this.buffer[w++] = (codePoint & 0x3F) | 0x80;
            }
        }

        // Count how many bytes we wrote.
        const written = w - start;

        // Write the length prefix, then skip over it and the written string.
        this.view.setUint32(this.length, written, true);
        this.length += 4 + written;
    }

    readGuid(): Guid {
      const guid = Guid.fromBytes(this.buffer, this.index);
      this.index += 16;
      return guid;
    }


    writeGuid(value: Guid): void {
        const i = this.length;
        this.growBy(16);
        value.writeToView(this.view, i);
    }

    // A note on these numbers:
    // 62135596800000 ms is the difference between the C# epoch (0001-01-01) and the Unix epoch (1970-01-01).
    // 0.0001 is the number of milliseconds per "tick" (a tick is 100 ns).
    // 429496.7296 is the number of milliseconds in 2^32 ticks.
    // 0x3fffffff is a mask to ignore the "Kind" bits of the Date.ToBinary value.
    // 0x40000000 is a mask to set the "Kind" bits to "DateTimeKind.Utc".

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
const typeMarker = '#btype';
const keyMarker = '#ktype';
const mapTag = 1;
const dateTag = 2;
const uint8ArrayTag = 3;
const bigIntTag = 4;
const guidTag = 5;
const mapGuidTag = 6;
const boolTag = 7;
const stringTag = 8;
const numberTag = 9;
const unknownTag = 10;

const castScalarByTag = (value: any, tag: number): any => {
  switch (tag) {
    case bigIntTag:
      return BigInt(value);
    case boolTag:
      return Boolean(value);
    case stringTag:
      return value;
    case numberTag:
      return Number(value);
    default:
      throw new BebopRuntimeError(`Unknown scalar tag: ${tag}`);
  }
};

const isPrimitive = (value: any): boolean => {
    const type = typeof value;
    return (
        value === null ||
        type === 'string' ||
        type === 'number' ||
        type === 'boolean'
    );
};

/**
 * A custom replacer function for JSON.stringify that supports BigInt, Map,
 * Date, Uint8Array, including BigInt values inside Map and Array.
 * @param _key - The key of the property being stringified.
 * @param value - The value of the property being stringified.
 * @returns The modified value for the property, or the original value if not a BigInt or Map.
 */
const replacer = (_key: string, value: any): any => {
    if (typeof value === "bigint") {
        return { [typeMarker]: bigIntTag, value: value.toString() };
    }
    if (value instanceof Date) {
        const ms = BigInt(value.getTime());
        const ticks = ms * 10000n + ticksBetweenEpochs;
        return { [typeMarker]: dateTag, value: (ticks & dateMask).toString() };
    }
    if (value instanceof Uint8Array) {
        return { [typeMarker]: uint8ArrayTag, value: Array.from(value) };
    }
    if (value instanceof GuidMap) {
        const obj: Record<any, any> = {};
        for (let [k, v] of value.entries()) {
            const guid = k.toString();
            obj[guid.toString()] = replacer(guid, v);
        }
        return { [typeMarker]: mapGuidTag, value: obj };
    }
    if (value instanceof Map) {
        const obj: Record<any, any> = {};
        let keyTag = unknownTag;
        const keyType = typeof value.keys().next().value;
        switch (keyType) {
            case "string":
                keyTag = stringTag;
                break;
            case "number":
                keyTag = numberTag;
                break;
            case "boolean":
                keyTag = boolTag;
                break;
            case "bigint":
                keyTag = bigIntTag;
                break;
            default:
                throw new BebopRuntimeError(`Not suitable map type tag found. Keys must be strings, numbers, booleans, or BigInts: ${keyType}`);
        }
        for (let [k, v] of value.entries()) {
            obj[k] = replacer(k, v);
        }
        return { [typeMarker]: mapTag, [keyMarker]: keyTag, value: obj };
    }
    if (value instanceof Guid) {
        return { [typeMarker]: guidTag, value: value.toString() };
    }
    if (Array.isArray(value)) {
        if (value.every(isPrimitive)) {
            return value;
        } else {
            const newValue = [];
            for (let i = 0; i < value.length; i++) {
                newValue[i] = replacer(i.toString(), value[i]);
            }
            return newValue;
        }
    }
    if (typeof value === "object" && value !== null) {
        for (let k in value) {
            if (value.hasOwnProperty(k) && !isPrimitive(value[k])) {
                const obj: Record<any, any> = {};
                for (let k in value) {
                    if (value.hasOwnProperty(k)) {
                        obj[k] = replacer(k, value[k]);
                    }
                }
                return obj;
            }
        }
        return value;
    }
    return value;
};

/**
 * A custom reviver function for JSON.parse that supports BigInt, Map, Date,
 * Uint8Array, including nested values
 * @param _key - The key of the property being parsed.
 * @param value - The value of the property being parsed.
 * @returns The modified value for the property, or the original value if not a marked type.
 */
const reviver = (_key: string, value: any): any => {
    if (value && typeof value === "object") {
        if (value[typeMarker]) {
            switch (value[typeMarker]) {
                case bigIntTag:
                    return BigInt(value.value);
                case dateTag:
                    const ticks = BigInt(value.value) & dateMask;
                    const ms = (ticks - ticksBetweenEpochs) / 10000n;
                    return new Date(Number(ms));
                case uint8ArrayTag:
                    return new Uint8Array(value.value);
                case mapTag:
                    const keyTag = value[keyMarker];
                    if (keyTag === undefined) {
                        throw new BebopRuntimeError("Map key type tag not found.");
                    }
                    const map = new Map();
                    for (let k in value.value) {
                        if (value.value.hasOwnProperty(k)) {
                            const trueKey = castScalarByTag(k, keyTag);
                            map.set(trueKey, reviver(k, value.value[k]));
                        }
                    }
                    return map;
                case guidTag:
                    return Guid.parseGuid(value.value);
                    case mapGuidTag:
                    const guidMap = new GuidMap();
                    for (let k in value.value) {
                        if (value.value.hasOwnProperty(k)) {
                            guidMap.set(Guid.parseGuid(k), reviver(k, value.value[k]));
                        }
                    }
                    return guidMap;
                default:
                    throw new BebopRuntimeError(`Unknown type marker: ${value[typeMarker]}`)
            }
        } else {
            if (Object.values(value).every(isPrimitive)) {
                return value;
            } else {
                for (let k in value) {
                    if (value.hasOwnProperty(k)) {
                        value[k] = reviver(k, value[k]);
                    }
                }
            }
        }
    }
    return value;
};


/**
 * Checks if the given keys exist in the given object.
 * @param keyPaths - An array of dot-separated key paths to check for existence.
 * @param obj - The object to check for key existence.
 * @returns An object with boolean values indicating whether each key path exists in the object.
 */
const keysExist = (
    keyPaths: string[],
    obj: Record<string, any>
): Record<string, boolean> => {
    return keyPaths.reduce((acc, keyPath) => {
        let current = obj;
        const keys = keyPath.split(".");

        for (let i = 0; i < keys.length; i++) {
            if (current[keys[i]] === undefined) {
                acc[keyPath] = false;
                return acc;
            } else {
                current = current[keys[i]];
            }
        }
        acc[keyPath] = true;
        return acc;
    }, {} as Record<string, boolean>);
};

/**
 * Ensures that the given keys exist in the given object, throwing an error if any key is missing.
 * @param keyPaths - An array of dot-separated key paths to check for existence.
 * @param obj - The object to check for key existence.
 * @throws {BebopRuntimeError} - If a required key path does not exist in the parsed object.
 */
const ensureKeysExist = (
    keyPaths: string[],
    obj: Record<string, any>
): void => {
    const results = keysExist(keyPaths, obj);
    for (let key in results) {
        if (!results[key]) {
            throw new BebopRuntimeError(`Error while parsing JSON. Required Key path ${key} does not exist in the parsed object.`);
        }
    }
}

/**
 * A collection of functions for working with Bebop-encoded JSON.
 */
export const BebopJson = {
    /**
     * A custom replacer function for JSON.stringify that supports BigInt, Map,
     * Date, Uint8Array, including BigInt values inside Map and Array.
     * @param _key - The key of the property being stringified.
     * @param value - The value of the property being stringified.
     * @returns The modified value for the property, or the original value if not a BigInt or Map.
     */
    replacer,

    /**
     * A custom reviver function for JSON.parse that supports BigInt, Map, Date,
     * Uint8Array, including nested values
     * @param _key - The key of the property being parsed.
     * @param value - The value of the property being parsed.
     * @returns The modified value for the property, or the original value if not a marked type.
     */
    reviver,

    /**
     * Checks if the given keys exist in the given object.
     * @param keyPaths - An array of dot-separated key paths to check for existence.
     * @param obj - The object to check for key existence.
     * @returns An object with boolean values indicating whether each key path exists in the object.
     */
    keysExist,

    /**
     * Ensures that the given keys exist in the given object, throwing an error if any key is missing.
     * @param keyPaths - An array of dot-separated key paths to check for existence.
     * @param obj - The object to check for key existence.
     * @throws {BebopRuntimeError} - If a required key path does not exist in the parsed object.
     */
    ensureKeysExist
}

/**
 * Ensures that the given value is a valid boolean.
 * @param value - The value to check.
 * @throws {BebopRuntimeError} - If the value is not a valid boolean.
 */
const ensureBoolean = (value: any): void => {
    if (!(value === false || value === true || value instanceof Boolean || typeof value === "boolean")) {
        throw new BebopRuntimeError(`Invalid value for Boolean: ${value} / typeof ${typeof value}`);
    }
}

/**
 * Ensures that the given value is a valid Uint8 number (0 to 255).
 * @param value - The value to check.
 * @throws {BebopRuntimeError} - If the value is not a valid Uint8 number.
 */
const ensureUint8 = (value: any): void => {
    if (!Number.isInteger(value) || value < 0 || value > 255) {
        throw new BebopRuntimeError(`Invalid value for Uint8: ${value}`);
    }
}


/**
 * Ensures that the given value is a valid Int16 number (-32768 to 32767).
 * @param value - The value to check.
 * @throws {BebopRuntimeError} - If the value is not a valid Int16 number.
 */
const ensureInt16 = (value: any): void => {
    if (!Number.isInteger(value) || value < -32768 || value > 32767) {
        throw new BebopRuntimeError(`Invalid value for Int16: ${value}`);
    }
}

/**
 * Ensures that the given value is a valid Uint16 number (0 to 65535).
 * @param value - The value to check.
 * @throws {BebopRuntimeError} - If the value is not a valid Uint16 number.
 */
const ensureUint16 = (value: any): void => {
    if (!Number.isInteger(value) || value < 0 || value > 65535) {
        throw new BebopRuntimeError(`Invalid value for Uint16: ${value}`);
    }
}
/**
 * Ensures that the given value is a valid Int32 number (-2147483648 to 2147483647).
 * @param value - The value to check.
 * @throws {BebopRuntimeError} - If the value is not a valid Int32 number.
 */
const ensureInt32 = (value: any): void => {
    if (!Number.isInteger(value) || value < -2147483648 || value > 2147483647) {
        throw new BebopRuntimeError(`Invalid value for Int32: ${value}`);
    }
}

/**
* Ensures that the given value is a valid Uint32 number (0 to 4294967295).
* @param value - The value to check.
* @throws {BebopRuntimeError} - If the value is not a valid Uint32 number.
*/
const ensureUint32 = (value: any): void => {
    if (!Number.isInteger(value) || value < 0 || value > 4294967295) {
        throw new BebopRuntimeError(`Invalid value for Uint32: ${value}`);
    }
}
/**
* Ensures that the given value is a valid Int64 number (-9223372036854775808 to 9223372036854775807).
* @param value - The value to check.
* @throws {BebopRuntimeError} - If the value is not a valid Int64 number.
*/
const ensureInt64 = (value: bigint | number): void => {
    const min = BigInt("-9223372036854775808");
    const max = BigInt("9223372036854775807");
    value = BigInt(value);
    if (value < min || value > max) {
        throw new BebopRuntimeError(`Invalid value for Int64: ${value}`);
    }
}
/**
* Ensures that the given value is a valid Uint64 number (0 to 18446744073709551615).
* @param value - The value to check.
* @throws {BebopRuntimeError} - If the value is not a valid Uint64 number.
*/
const ensureUint64 = (value: bigint | number): void => {
    const max = BigInt("18446744073709551615");
    value = BigInt(value);
    if (value < BigInt(0) || value > max) {
        throw new BebopRuntimeError(`Invalid value for Uint64: ${value}`);
    }
}

/**
 * Ensures that the given value is a valid BigInt number.
 * @param value - The value to check.
 * @throws {BebopRuntimeError} - If the value is not a valid BigInt number.
 */
const ensureBigInt = (value: any): void => {
    if (typeof value !== 'bigint') {
        throw new BebopRuntimeError(`Invalid value for BigInt: ${value}`);
    }
}


/**
 * Ensures that the given value is a valid float number.
 * @param value - The value to check.
 * @throws {Error} - If the value is not a valid float number.
 */
const ensureFloat = (value: any): void => {
    if (typeof value !== 'number' || !Number.isFinite(value)) {
        throw new BebopRuntimeError(`Invalid value for Float: ${value}`);
    }
}


/**
 * Ensures that the given value is a valid Map object, with keys and values that pass the specified validators.
 * @param value - The value to check.
 * @param keyTypeValidator - A function that validates the type of each key in the Map.
 * @param valueTypeValidator - A function that validates the type of each value in the Map.
 * @throws {BebopRuntimeError} - If the value is not a valid Map object, or if any key or value fails validation.
 */
const ensureMap = (value: any, keyTypeValidator: (key: any) => void, valueTypeValidator: (value: any) => void): void => {
    if (!(value instanceof Map || value instanceof GuidMap)) {
        throw new BebopRuntimeError(`Invalid value for Map: ${value}`);
    }
    for (let [k, v] of value) {
        keyTypeValidator(k);
        valueTypeValidator(v);
    }
}

/**
 * Ensures that the given value is a valid Array object, with elements that pass the specified validator.
 * @param value - The value to check.
 * @param elementTypeValidator - A function that validates the type of each element in the Array.
 * @throws {BebopRuntimeError} - If the value is not a valid Array object, or if any element fails validation.
 */
const ensureArray = (value: any, elementTypeValidator: (element: any) => void): void => {
    if (!Array.isArray(value)) {
        throw new BebopRuntimeError(`Invalid value for Array: ${value}`);
    }
    for (let element of value) {
        elementTypeValidator(element);
    }
}

/**
 * Ensures that the given value is a valid Date object.
 * @param value - The value to check.
 * @throws {BebopRuntimeError} - If the value is not a valid Date object.
 */
const ensureDate = (value: any): void => {
    if (!(value instanceof Date)) {
        throw new BebopRuntimeError(`Invalid value for Date: ${value}`);
    }
}

/**
 * Ensures that the given value is a valid Uint8Array object.
 * @param value - The value to check.
 * @throws {BebopRuntimeError} - If the value is not a valid Uint8Array object.
 */
const ensureUint8Array = (value: any): void => {
    if (!(value instanceof Uint8Array)) {
        throw new BebopRuntimeError(`Invalid value for Uint8Array: ${value}`);
    }
}

/**
 * Ensures that the given value is a valid string.
 * @param value - The value to check.
 * @throws {BebopRuntimeError} - If the value is not a valid string.
 */
const ensureString = (value: any): void => {
    if (typeof value !== 'string') {
        throw new BebopRuntimeError(`Invalid value for String: ${value}`);
    }
}


/**
 * Ensures that the given value is a valid enum value.
 * @param value - The value to check.
 * @param enumValue - An object representing the enum values.
 * @throws {BebopRuntimeError} - If the value is not a valid enum value.
 */
const ensureEnum = (value: any, enumValue: object): void => {
    if (!Number.isInteger(value)) {
        throw new BebopRuntimeError(`Invalid value for enum, not an int: ${value}`);
    }
    if (!(value in enumValue)) {
        throw new BebopRuntimeError(`Invalid value for enum, not in enum: ${value}`);
    }
}

/**
 * Ensures that the given value is a valid Guid object.
 * @param value - The value to check.
 * @throws {BebopRuntimeError} - If the value is not a valid Guid object.
 */
const ensureGuid = (value: any): void => {
    if (!(value instanceof Guid)) {
        throw new BebopRuntimeError(`Invalid value for Guid: ${value}`);
    }
}


/**
 * This object contains functions for ensuring that values conform to specific types.
 */
export const BebopTypeGuard = {
    /**
     * Ensures that the given value is a valid boolean.
     * @param value - The value to check.
     * @throws {BebopRuntimeError} - If the value is not a valid boolean.
     */
    ensureBoolean,
    /**
     * Ensures that the given value is a valid Uint8 number (0 to 255).
     * @param value - The value to check.
     * @throws {BebopRuntimeError} - If the value is not a valid Uint8 number.
     */
    ensureUint8,
    /**
     * Ensures that the given value is a valid Int16 number (-32768 to 32767).
     * @param value - The value to check.
     * @throws {BebopRuntimeError} - If the value is not a valid Int16 number.
     */
    ensureInt16,
    /**
     * Ensures that the given value is a valid Uint16 number (0 to 65535).
     * @param value - The value to check.
     * @throws {BebopRuntimeError} - If the value is not a valid Uint16 number.
     */
    ensureUint16,
    /**
     * Ensures that the given value is a valid Int32 number (-2147483648 to 2147483647).
     * @param value - The value to check.
     * @throws {BebopRuntimeError} - If the value is not a valid Int32 number.
     */
    ensureInt32,
    /**
     * Ensures that the given value is a valid Uint32 number (0 to 4294967295).
     * @param value - The value to check.
     * @throws {BebopRuntimeError} - If the value is not a valid Uint32 number.
     */
    ensureUint32,
    /**
     * Ensures that the given value is a valid Int64 number (-9223372036854775808 to 9223372036854775807).
     * @param value - The value to check.
     * @throws {BebopRuntimeError} - If the value is not a valid Int64 number.
     */
    ensureInt64,
    /**
     * Ensures that the given value is a valid Uint64 number (0 to 18446744073709551615).
     * @param value - The value to check.
     * @throws {BebopRuntimeError} - If the value is not a valid Uint64 number.
     */
    ensureUint64,
    /**
     * Ensures that the given value is a valid BigInt number.
     * @param value - The value to check.
     * @throws {BebopRuntimeError} - If the value is not a valid BigInt number.
     */
    ensureBigInt,
    /**
     * Ensures that the given value is a valid float number.
     * @param value - The value to check.
     * @throws {BebopRuntimeError} - If the value is not a valid float number.
     */
    ensureFloat,
    /**
     * Ensures that the given value is a valid Map object, with keys and values that pass the specified validators.
     * @param value - The value to check.
     * @param keyTypeValidator - A function that validates the type of each key in the Map.
     * @param valueTypeValidator - A function that validates the type of each value in the Map.
     * @throws {BebopRuntimeError} - If the value is not a valid Map object, or if any key or value fails validation.
     */
    ensureMap,
    /**
     * Ensures that the given value is a valid Array object, with elements that pass the specified validator.
     * @param value - The value to check.
     * @param elementTypeValidator - A function that validates the type of each element in the Array.
     * @throws {BebopRuntimeError} - If the value is not a valid Array object, or if any element fails validation.
     */
    ensureArray,
    /**
     * Ensures that the given value is a valid Date object.
     * @param value - The value to check.
     * @throws {BebopRuntimeError} - If the value is not a valid Date object.
     */
    ensureDate,
    /**
     * Ensures that the given value is a valid Uint8Array object.
     * @param value - The value to check.
     * @throws {BebopRuntimeError} - If the value is not a valid Uint8Array object.
     */
    ensureUint8Array,
    /**
     * Ensures that the given value is a valid string.
     * @param value - The value to check.
     * @throws {BebopRuntimeError} - If the value is not a valid string.
     */
    ensureString,
    /**
     * Ensures that the given value is a valid enum value.
     * @param value - The value to check.
     * @param enumValues - An array of valid enum values.
     * @throws {BebopRuntimeError} - If the value is not a valid enum value.
     */
    ensureEnum,
    /**
     * Ensures that the given value is a valid GUID string.
     * @param value - The value to check.
     * @throws {BebopRuntimeError} - If the value is not a valid GUID string.
     */
    ensureGuid
}

