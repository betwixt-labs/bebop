import {
  BebopRuntimeError,
  BebopTypeGuard,
  BebopView,
  Guid,
  GuidMap,
} from "./index";

const decoder = new TextDecoder();

type FieldTypes =
  | { type: "scalar" }
  | {
      type: "array";
      memberTypeId: number;
      depth: number;
    }
  | {
      type: "map";
      keyTypeId: number;
      valueTypeId: number;
      nestedType?: FieldTypes;
    };

enum WireMethodType {
  Unary = 0,
  ServerStreaming = 1,
  ClientStreaming = 2,
  DuplexStream = 3,
}
enum WireBaseType {
  Bool = -1,
  Byte = -2,
  UInt16 = -3,
  Int16 = -4,
  UInt32 = -5,
  Int32 = -6,
  UInt64 = -7,
  Int64 = -8,
  Float32 = -9,
  Float64 = -10,
  String = -11,
  Guid = -12,
  Date = -13,
}

enum WireTypeKind {
  Struct = 1,
  Message,
  Union,
  Enum,
}

type Attributes = { [name: string]: Attribute };
interface Attribute {
  attributeName: string;
  attributeValue: string | null;
  isNumber: boolean;
}

interface EnumMember {
  name: string;
  attributes: Attributes;
  value: number | BigInt | null;
}

interface Field {
  name: string;
  typeId: number;
  fieldProperties: FieldTypes;
  attributes: Attributes;
  constantValue?: number | BigInt | null;
}

interface Definition {
  index: number;
  name: string;
  kind: WireTypeKind;
  attributes: Attributes;
}

interface Enum extends Definition {
  baseType: WireBaseType;
  members: { [name: string]: EnumMember };
}

interface Struct extends Definition {
  isReadOnly: boolean;
  fields: { [fieldName: string]: Field };
}

interface Message extends Definition {
  fields: { [fieldName: string]: Field };
}

interface UnionBranch {
  discriminator: number;
  typeId: number;
}

interface Union extends Definition {
  branchCount: number;
  branches: UnionBranch[];
}

interface Service {
  name: string;
  attributes: Attributes;
  methods: { [methodName: string]: ServiceMethod };
}

interface ServiceMethod {
  name: string;
  attributes: Attributes;
  requestTypeId: number;
  responseTypeId: number;
  methodType: WireMethodType;
  id: number;
}

interface ParsedSchema {
  bebopVersion: number;
  definedTypes: { [typeName: string]: Definition };
  services?: { [serviceName: string]: Service };
}

/**
 * A class that can read a buffer containing a Bebop encoded record by utilizing a binary schema.
 */
export class RecordReader {
  /**
   * @param schema - BinarySchema object containing metadata about Bebop schemas.
   * @private
   */
  private constructor(private readonly schema: BinarySchema) {}

  /**
   * Reads a Bebop encoded record from a buffer.
   *
   * @param definitionName - Name of the definition in the schema for the record to read.
   * @param data - The buffer to read the record from.
   * @returns - The read record as a Record object.
   * @throws - Throws an error if the record cannot be decoded directly.
   * @public
   */
  public read(
    definitionName: string,
    data: Uint8Array
  ): Record<string, unknown> {
    const definition = this.schema.getDefinition(definitionName);
    if (definition.kind === WireTypeKind.Enum) {
      throw new BebopRuntimeError("Cannot decode enum directly");
    }
    const view = BebopView.getInstance();
    view.startReading(data);
    return this.readDefinition(definition, view) as Record<string, unknown>;
  }

  private readDefinition(
    definition: Definition,
    view: BebopView
  ): number | bigint | Record<string, unknown> {
    switch (definition.kind) {
      case WireTypeKind.Enum:
        return this.readEnumDefinition(definition as Enum, view);
      case WireTypeKind.Union:
        return this.readUnionDefinition(definition as Union, view);
      case WireTypeKind.Struct:
        return this.readStructDefinition(definition as Struct, view);
      case WireTypeKind.Message:
        return this.readMessageDefinition(definition as Message, view);
      default:
        throw new BebopRuntimeError(`Unknown type kind: ${definition.kind}`);
    }
  }

  private readStructDefinition(definition: Struct, view: BebopView) {
    const record = {} as Record<string, unknown>;
    Object.values(definition.fields).forEach((field) => {
      record[field.name] = this.readField(field, view);
      if (!(field.name in record) || record[field.name] === undefined) {
        throw new BebopRuntimeError(`Missing field ${field.name}`);
      }
    });
    if (definition.isReadOnly) {
      Object.freeze(record);
    }
    return record;
  }

  private readMessageDefinition(definition: Message, view: BebopView) {
    const record = {} as Record<string, unknown>;
    const length = view.readMessageLength();
    const end = view.index + length;
    const fields = Object.values(definition.fields);
    while (true) {
      const discriminator = view.readByte();
      if (discriminator === 0) {
        return record;
      }
      const field = fields.find((f) => f.constantValue === discriminator);
      if (field === undefined) {
        view.index = end;
        return record;
      }
      record[field.name] = this.readField(field, view);
    }
  }

  private readField(field: Field, view: BebopView) {
    if (field.typeId >= 0) {
      const definition = this.schema.getDefinition(field.typeId);
      return this.readDefinition(definition, view);
    }
    switch (field.fieldProperties.type) {
      case "scalar":
        return this.readScalar(field.typeId, view);
      case "array":
        return this.readArray(
          field.fieldProperties,
          field.fieldProperties.depth,
          view
        );
      case "map":
        return this.readMap(field.fieldProperties, view);
      default:
        throw new BebopRuntimeError(
          `Unknown field type: ${field.fieldProperties}`
        );
    }
  }

  private readScalar(
    typeId: WireBaseType,
    view: BebopView
  ): boolean | number | string | Date | bigint | Guid {
    switch (typeId) {
      case WireBaseType.Bool:
        return !!view.readByte();
      case WireBaseType.Byte:
        return view.readByte();
      case WireBaseType.UInt16:
        return view.readUint16();
      case WireBaseType.Int16:
        return view.readInt16();
      case WireBaseType.UInt32:
        return view.readUint32();
      case WireBaseType.Int32:
        return view.readInt32();
      case WireBaseType.UInt64:
        return view.readUint64();
      case WireBaseType.Int64:
        return view.readInt64();
      case WireBaseType.Float32:
        return view.readFloat32();
      case WireBaseType.Float64:
        return view.readFloat64();
      case WireBaseType.String:
        return view.readString();
      case WireBaseType.Date:
        return view.readDate();
      case WireBaseType.Guid:
        return view.readGuid();
      default:
        throw new BebopRuntimeError(`Unknown scalar type: ${typeId}`);
    }
  }

  private readArray(
    field: FieldTypes,
    depth: number,
    view: BebopView
  ): Array<unknown> | Uint8Array {
    if (field.type !== "array") {
      throw new BebopRuntimeError(`Expected array field, got ${field.type}`);
    }
    const memberType = field.memberTypeId;
    // Recursive case: there is further nesting.
    if (depth > 0) {
      const length = view.readUint32();
      const array = new Array(length);
      for (let i = 0; i < length; i++) {
        array[i] = this.readArray(field, depth - 1, view);
      }
      return array;
    }
    // Base case: no further nesting. Decode items using the appropriate method.
    if (memberType === WireBaseType.Byte) {
      return view.readBytes();
    }
    let definition;
    if (memberType >= 0) {
      definition = this.schema.getDefinition(memberType);
    }
    const length = view.readUint32();
    const array = new Array(length);
    for (let i = 0; i < length; i++) {
      if (definition !== undefined) {
        array[i] = this.readDefinition(definition, view);
      } else {
        array[i] = this.readScalar(memberType, view);
      }
    }
    return array;
  }

  private readMap(
    field: FieldTypes,
    view: BebopView
  ): Map<unknown, unknown> | GuidMap<unknown> {
    if (field.type !== "map") {
      throw new BebopRuntimeError(`Expected map field, got ${field.type}`);
    }

    const keyType = field.keyTypeId;
    const valueType = field.valueTypeId;
    const map =
      field.keyTypeId === WireBaseType.Guid
        ? new GuidMap<unknown>()
        : new Map<unknown, unknown>();
    const size = view.readUint32();
    let definition;
    if (valueType >= 0) {
      definition = this.schema.getDefinition(valueType);
    }
    for (let i = 0; i < size; i++) {
      const key = this.readScalar(keyType, view);
      let value;
      if (definition !== undefined) {
        value = this.readDefinition(definition, view);
      } else if (field.nestedType !== undefined) {
        const nested = field.nestedType;
        if (nested.type === "array") {
          value = this.readArray(nested, nested.depth, view);
        } else if (nested.type === "map") {
          value = this.readMap(nested, view);
        }
      } else {
        value = this.readScalar(valueType, view);
      }
      if (value === undefined) {
        throw new BebopRuntimeError(`Error decoding map value for key ${key}`);
      }
      // @ts-ignore
      map.set(key, value);
    }
    return map;
  }

  private readEnumDefinition(
    definition: Enum,
    view: BebopView
  ): number | bigint {
    switch (definition.baseType) {
      case WireBaseType.Byte:
        return view.readByte();
      case WireBaseType.UInt16:
        return view.readUint16();
      case WireBaseType.Int16:
        return view.readInt16();
      case WireBaseType.UInt32:
        return view.readUint32();
      case WireBaseType.Int32:
        return view.readInt32();
      case WireBaseType.UInt64:
        return view.readUint64();
      case WireBaseType.Int64:
        return view.readInt64();
      default:
        throw new BebopRuntimeError(
          `Unknown enum base type: ${definition.baseType}`
        );
    }
  }

  private readUnionDefinition(definition: Union, view: BebopView) {
    const length = view.readMessageLength();
    const end = view.index + 1 + length;
    const discriminator = view.readByte();
    const branch = definition.branches.find(
      (b) => b.discriminator === discriminator
    );
    if (branch === undefined) {
      view.index = end;
      throw new BebopRuntimeError(`Unknown discriminator: ${discriminator}`);
    }
    return {
      discriminator,
      value: this.readDefinition(
        this.schema.getDefinition(branch.typeId),
        view
      ),
    };
  }
}

/**
 * A class responsible for writing a dynamic record into a Bebop buffer.
 * The class uses a binary schema provided during instantiation to encode the data.
 *
 * @example
 * const writer = binarySchema.writer;
 * const buffer = writer.write('DefinitionName', record);
 */
export class RecordWriter {
  /**
   * @param schema Binary schema used for encoding the data.
   * @private
   */
  private constructor(private schema: BinarySchema) {}
  /**
   * Encodes a given record according to a provided definition name and returns it as a Uint8Array.
   *
   * @param definitionName Name of the definition to be used for encoding.
   * @param record The record to be encoded.
   * @returns Encoded record as a Uint8Array.
   */
  public write(
    definitionName: string,
    record: Record<string, unknown>
  ): Uint8Array {
    const definition = this.schema.getDefinition(definitionName);
    const view = BebopView.getInstance();
    view.startWriting();
    this.writeDefinition(definition, view, record);
    return view.toArray();
  }

  private writeDefinition(
    definition: Definition,
    view: BebopView,
    record: unknown
  ): void {
    switch (definition.kind) {
      case WireTypeKind.Enum:
        this.writeEnumDefinition(definition as Enum, view, record);
        break;
      case WireTypeKind.Union:
        this.writeUnionDefinition(definition as Union, view, record);
        break;
      case WireTypeKind.Struct:
        this.writeStructDefinition(definition as Struct, view, record);
        break;
      case WireTypeKind.Message:
        this.writeMessageDefinition(definition as Message, view, record);
        break;
    }
  }

  private writeStructDefinition(
    definition: Struct,
    view: BebopView,
    record: unknown
  ): number {
    if (!this.isRecord(record)) {
      throw new BebopRuntimeError(`Expected object, got ${typeof record}`);
    }
    const before = view.length;
    Object.values(definition.fields).forEach((field) => {
      if (!(field.name in record)) {
        throw new BebopRuntimeError(`Missing field: ${field.name}`);
      }
      if (record[field.name] === undefined) {
        throw new BebopRuntimeError(`Field ${field.name} is undefined`);
      }
      this.writeField(field, view, record[field.name]);
    });
    const after = view.length;
    return after - before;
  }

  private writeMessageDefinition(
    definition: Message,
    view: BebopView,
    record: unknown
  ) {
    if (!this.isRecord(record)) {
      throw new BebopRuntimeError(`Expected object, got ${typeof record}`);
    }
    const before = view.length;
    const pos = view.reserveMessageLength();
    const start = view.length;
    Object.values(definition.fields).forEach((field) => {
      if (field.constantValue === undefined || field.constantValue === null) {
        throw new BebopRuntimeError(
          `Missing constant value for field: ${field.name}`
        );
      }
      if (typeof field.constantValue !== "number") {
        throw new BebopRuntimeError(
          `Expected number, got ${typeof field.constantValue} for field: ${
            field.name
          }`
        );
      }
      if (field.name in record && record[field.name] !== undefined) {
        view.writeByte(field.constantValue);
        this.writeField(field, view, record[field.name]);
      }
    });
    view.writeByte(0);
    const end = view.length;
    view.fillMessageLength(pos, end - start);
    const after = view.length;
    return after - before;
  }

  private writeEnumDefinition(
    definition: Enum,
    view: BebopView,
    value: unknown
  ): void {
    if (typeof value !== "number" && typeof value !== "bigint") {
      throw new BebopRuntimeError(
        `Expected number or bigint, got ${typeof value}`
      );
    }
    if (
      (definition.baseType === WireBaseType.Int64 ||
        definition.baseType === WireBaseType.UInt64) &&
      typeof value !== "bigint"
    ) {
      throw new BebopRuntimeError(`Expected bigint, got ${typeof value}`);
    }
    let valueFound = false;
    for (const member in definition.members) {
      if (definition.members[member].value === value) {
        valueFound = true;
        break;
      }
    }
    if (!valueFound) {
      throw new BebopRuntimeError(
        `Enum '${definition.name}' does not contain value: ${value}`
      );
    }
    switch (definition.baseType) {
      case WireBaseType.Byte:
        BebopTypeGuard.ensureUint8(value);
        view.writeByte(value as number);
        break;
      case WireBaseType.UInt16:
        BebopTypeGuard.ensureUint16(value);
        view.writeUint16(value as number);
        break;
      case WireBaseType.Int16:
        BebopTypeGuard.ensureInt16(value);
        view.writeInt16(value as number);
        break;
      case WireBaseType.UInt32:
        BebopTypeGuard.ensureUint32(value);
        view.writeUint32(value as number);
        break;
      case WireBaseType.Int32:
        BebopTypeGuard.ensureInt32(value);
        view.writeInt32(value as number);
        break;
      case WireBaseType.UInt64:
        BebopTypeGuard.ensureUint64(value);
        view.writeUint64(value as bigint);
        break;
      case WireBaseType.Int64:
        BebopTypeGuard.ensureInt64(value);
        view.writeInt64(value as bigint);
        break;
      default:
        throw new BebopRuntimeError(
          `Unknown enum base type: ${definition.baseType}`
        );
    }
  }

  private writeUnionDefinition(
    definition: Union,
    view: BebopView,
    record: unknown
  ): number {
    if (record === null || record === undefined || typeof record !== "object") {
      throw new BebopRuntimeError(`Expected non-null object value`);
    }
    if (
      !("discriminator" in record && typeof record.discriminator === "number")
    ) {
      throw new BebopRuntimeError(`Expected number 'discriminator' property`);
    }
    if (
      !(
        "value" in record &&
        record.value !== null &&
        typeof record.value === "object"
      )
    ) {
      throw new BebopRuntimeError(`Expected 'value' property`);
    }
    const branch = definition.branches.find(
      (b) => b.discriminator === record.discriminator
    );
    if (branch === undefined) {
      throw new BebopRuntimeError(
        `No branch found for discriminator: ${record.discriminator}`
      );
    }
    const branchDefinition = this.schema.getDefinition(branch.typeId);

    const before = view.length;
    const pos = view.reserveMessageLength();
    const start = view.length + 1;
    view.writeByte(record.discriminator);
    this.writeDefinition(branchDefinition, view, record.value);
    const end = view.length;
    view.fillMessageLength(pos, end - start);
    const after = view.length;
    return after - before;
  }

  private writeField(field: Field, view: BebopView, value: unknown): void {
    if (field.typeId >= 0) {
      const definition = this.schema.getDefinition(field.typeId);
      this.writeDefinition(definition, view, value);
      return;
    }
    switch (field.fieldProperties.type) {
      case "scalar":
        this.writeScalar(field.typeId, view, value);
        break;
      case "array":
        this.writeArray(
          field.fieldProperties,
          field.fieldProperties.depth,
          view,
          value
        );
        break;
      case "map":
        this.writeMap(field.fieldProperties, view, value);
        break;
      default:
        throw new BebopRuntimeError(
          `Unknown field type: ${field.fieldProperties}`
        );
    }
  }

  private writeArray(
    field: FieldTypes,
    depth: number,
    view: BebopView,
    value: unknown
  ): void {
    if (field.type !== "array") {
      throw new BebopRuntimeError(`Expected array field, got ${field.type}`);
    }
    if (!Array.isArray(value) && !(value instanceof Uint8Array)) {
      throw new BebopRuntimeError(`Expected array, got ${typeof value}`);
    }
    if (
      field.memberTypeId === WireBaseType.Byte &&
      !(value instanceof Uint8Array)
    ) {
      throw new BebopRuntimeError(`Expected Uint8Array, got ${typeof value}`);
    }

    const memberType = field.memberTypeId;
    const length = value.length;
    // Recursive case: there is further nesting.
    if (depth > 0) {
      view.writeUint32(length);
      for (let i = 0; i < length; i++) {
        this.writeArray(field, depth - 1, view, value[i]);
      }
      return;
    }
    // Base case: no further nesting. Encode items using the appropriate method.
    if (memberType === WireBaseType.Byte) {
      view.writeBytes(value as Uint8Array);
    } else {
      view.writeUint32(length);
      let definition;
      if (memberType >= 0) {
        definition = this.schema.getDefinition(memberType);
      }
      for (let i = 0; i < length; i++) {
        if (definition !== undefined) {
          this.writeDefinition(definition, view, value[i]);
        } else {
          this.writeScalar(memberType, view, value[i]);
        }
      }
    }
  }

  private writeMap(field: FieldTypes, view: BebopView, value: unknown): void {
    if (field.type !== "map") {
      throw new BebopRuntimeError(`Expected map field, got ${field.type}`);
    }
    if (!(value instanceof Map || value instanceof GuidMap)) {
      throw new BebopRuntimeError(`Expected Map, got ${typeof value}`);
    }
    const keyType = field.keyTypeId;
    const valueType = field.valueTypeId;
    const size = value.size;
    view.writeUint32(size);
    let definition;
    if (valueType >= 0) {
      definition = this.schema.getDefinition(valueType);
    }
    for (const [k, v] of value.entries()) {
      this.writeScalar(keyType, view, k);
      if (definition !== undefined) {
        this.writeDefinition(definition, view, v);
      } else if (field.nestedType !== undefined) {
        const nested = field.nestedType;
        if (nested.type === "array") {
          this.writeArray(
            nested,
            nested.depth,
            view,
            v as Array<unknown> | Uint8Array
          );
        } else if (nested.type === "map") {
          this.writeMap(
            nested,
            view,
            v as Map<unknown, unknown> | GuidMap<unknown>
          );
        }
      } else {
        this.writeScalar(valueType, view, v);
      }
    }
  }

  private writeScalar(typeId: WireBaseType, view: BebopView, value: unknown) {
    switch (typeId) {
      case WireBaseType.Bool:
        BebopTypeGuard.ensureBoolean(value);
        view.writeByte(Number(value));
        break;
      case WireBaseType.Byte:
        BebopTypeGuard.ensureUint8(value);
        view.writeByte(value as number);
        break;
      case WireBaseType.UInt16:
        BebopTypeGuard.ensureUint16(value);
        view.writeUint16(value as number);
        break;
      case WireBaseType.Int16:
        BebopTypeGuard.ensureInt16(value);
        view.writeInt16(value as number);
        break;
      case WireBaseType.UInt32:
        BebopTypeGuard.ensureUint32(value);
        view.writeUint32(value as number);
        break;
      case WireBaseType.Int32:
        BebopTypeGuard.ensureInt32(value);
        view.writeInt32(value as number);
        break;
      case WireBaseType.UInt64:
        BebopTypeGuard.ensureUint64(value as bigint);
        view.writeUint64(value as bigint);
        break;
      case WireBaseType.Int64:
        BebopTypeGuard.ensureInt64(value as bigint);
        view.writeInt64(value as bigint);
        break;
      case WireBaseType.Float32:
        BebopTypeGuard.ensureFloat(value);
        view.writeFloat32(value as number);
        break;
      case WireBaseType.Float64:
        BebopTypeGuard.ensureFloat(value);
        view.writeFloat64(value as number);
        break;
      case WireBaseType.String:
        BebopTypeGuard.ensureString(value);
        view.writeString(value as string);
        break;
      case WireBaseType.Guid:
        BebopTypeGuard.ensureGuid(value);
        view.writeGuid(value as Guid);
        break;
      case WireBaseType.Date:
        BebopTypeGuard.ensureDate(value);
        view.writeDate(value as Date);
        break;
      default:
        throw new BebopRuntimeError(`Unknown scalar type: ${typeId}`);
    }
  }

  private isRecord(value: unknown): value is Record<string, unknown> {
    return value !== null && typeof value === "object";
  }
}

/**
 * `BinarySchema` represents a class that allows parsing of a Bebop schema in binary form.
 *
 * This class holds the DataView representation of the binary data, its parsing position,
 * and contains methods to parse each specific type of Bebop schema structure.
 */
export class BinarySchema {
  private readonly view: DataView;
  private readonly dataProxy: Uint8Array;
  private pos: number;
  private readonly ArrayType = -14;
  private readonly MapType = -15;
  private parsedSchema?: ParsedSchema;
  private indexToDefinition: { [index: number]: Definition } = {};
  private nameToDefinition: { [name: string]: Definition } = {};
  public reader: RecordReader;
  public writer: RecordWriter;

  /**
   * Create a new BinarySchema instance.
   * @param data - The binary data array.
   */
  constructor(private readonly data: Uint8Array) {
    // copy the data to prevent modification
    //this.data = data.subarray(0, data.length);
    this.view = new DataView(this.data.buffer);
    this.pos = 0;
    //@ts-expect-error
    this.reader = new RecordReader(this);
    //@ts-expect-error
    this.writer = new RecordWriter(this);
    this.dataProxy = new Proxy(this.data, {
      get: (target: Uint8Array, prop: PropertyKey): any => {
        // If prop is 'length', return the length of the Uint8Array
        if (prop === "length") {
          return target.length;
        }
        // If prop is a number-like string, convert it to a number and return the element at that index in the Uint8Array
        if (typeof prop === "string" && !isNaN(Number(prop))) {
          return target[Number(prop)];
        }
        // If prop is the name of a method of Uint8Array, return the function
        if (typeof prop === 'string' && typeof (target as any)[prop] === 'function') {
          return (target as any)[prop].bind(target);
        }
        // Optionally, you can throw an error or return undefined for all other properties
        throw new BebopRuntimeError(`Cannot access property ${String(prop)}`);
      },
      set: (_: Uint8Array, __: PropertyKey, ___: any): boolean => {
        throw new BebopRuntimeError("Cannot modify schema data");
      },
    });
  }

  /**
   * Parse the schema.
   * This method should only be called once per instance.
   */
  public parse(): void {
    if (this.parsedSchema !== undefined) {
      return;
    }

    const schemaVersion = this.parseUint8();
    const numDefinedTypes = this.parseUint32();

    let definedTypes: { [typeName: string]: Definition } = {};
    for (let i = 0; i < numDefinedTypes; i++) {
      const def = this.parseDefinedType(i);
      definedTypes[def.name] = def;
      this.indexToDefinition[i] = def;
      this.nameToDefinition[def.name] = def;
    }

    const serviceCount = this.parseUint32();
    let services: { [serviceName: string]: Service } = {};

    for (let i = 0; i < serviceCount; i++) {
      const service = this.parseServiceDefinition();
      services[service.name] = service;
    }
    this.parsedSchema = { bebopVersion: schemaVersion, definedTypes, services };
    Object.freeze(this.parsedSchema);
  }

  /**
   * Returns the parsed schema.
   */
  public get ast(): ParsedSchema {
    if (this.parsedSchema === undefined) {
      this.parse();
    }
    return this.parsedSchema!;
  }
  /**
   * Returns the raw binary data of the schema wrapped in an immutable Uint8Array.
   */
  public get raw(): Uint8Array {
    return this.dataProxy;
  }

  /**
   * Get a Definition by its index or name.
   * @param index - The index or name of the Definition.
   * @returns - The requested Definition.
   * @throws - Will throw an error if no Definition is found at the provided index.
   */
  public getDefinition(index: number | string): Definition {
    const definition =
      typeof index === "number"
        ? this.indexToDefinition[index]
        : this.nameToDefinition[index];
    if (!definition) {
      throw new BebopRuntimeError(`No definition found at index: ${index}`);
    }
    return definition;
  }

  private parseDefinedType(index: number): Definition {
    const typeName = this.parseString();
    const typeKind = this.parseUint8() as WireTypeKind;
    const typeAttributes = this.parseAttributes();

    switch (typeKind) {
      case WireTypeKind.Enum:
        return this.parseEnumDefinition(
          typeName,
          typeKind,
          typeAttributes,
          index
        );
      case WireTypeKind.Union:
        return this.parseUnionDefinition(
          typeName,
          typeKind,
          typeAttributes,
          index
        );
      case WireTypeKind.Struct:
        return this.parseStructDefinition(
          typeName,
          typeKind,
          typeAttributes,
          index
        );
      case WireTypeKind.Message:
        return this.parseMessageDefinition(
          typeName,
          typeKind,
          typeAttributes,
          index
        );
      default:
        throw new BebopRuntimeError(`Unknown type kind: ${typeKind}`);
    }
  }

  private parseAttributes() {
    const numAttributes = this.parseUint8();
    const attributes: { [name: string]: Attribute } = {};
    for (let i = 0; i < numAttributes; i++) {
      const attribute = this.parseAttribute();
      attributes[attribute.attributeName] = attribute;
    }
    return attributes;
  }

  private parseAttribute(): Attribute {
    const attributeName = this.parseString();
    const hasValue = this.parseBool();
    const attributeValue = hasValue ? this.parseString() : null;
    const isNumber = this.parseBool();
    return { attributeName, attributeValue, isNumber };
  }

  private parseEnumDefinition(
    typeName: string,
    typeKind: WireTypeKind,
    typeAttributes: Attributes,
    index: number
  ): Enum {
    const baseType = this.parseTypeId();
    const memberCount = this.parseUint8();
    const members: { [name: string]: EnumMember } = {};
    for (let i = 0; i < memberCount; i++) {
      const member = this.parseEnumMember(baseType);
      members[member.name] = member;
    }
    return {
      index,
      name: typeName,
      kind: typeKind,
      attributes: typeAttributes,
      baseType,
      members,
    };
  }

  private parseEnumMember(baseType: number): EnumMember {
    const name = this.parseString();
    const attributes = this.parseAttributes();
    const value = this.parseConstantValue(baseType);
    return { name, attributes, value };
  }

  private parseUnionDefinition(
    typeName: string,
    typeKind: WireTypeKind,
    typeAttributes: { [name: string]: Attribute },
    index: number
  ): Union {
    const branchCount = this.parseUint8();
    const branches = new Array(branchCount)
      .fill(null)
      .map(() => this.parseUnionBranch());
    return {
      index,
      name: typeName,
      kind: typeKind,
      attributes: typeAttributes,
      branchCount,
      branches,
    };
  }

  private parseUnionBranch(): UnionBranch {
    const discriminator = this.parseUint8();
    const typeId = this.parseTypeId();
    return { discriminator, typeId };
  }

  private parseStructDefinition(
    typeName: string,
    typeKind: WireTypeKind,
    typeAttributes: { [name: string]: Attribute },
    index: number
  ): Struct {
    const isReadOnly = this.parseBool();
    const fields = this.parseFields(typeKind);
    return {
      index,
      name: typeName,
      kind: typeKind,
      attributes: typeAttributes,
      isReadOnly,
      fields,
    };
  }

  private parseMessageDefinition(
    typeName: string,
    typeKind: WireTypeKind,
    typeAttributes: { [name: string]: Attribute },
    index: number
  ): Message {
    const fields = this.parseFields(typeKind);
    return {
      index,
      name: typeName,
      kind: typeKind,
      attributes: typeAttributes,
      fields,
    };
  }

  private parseFields(parentKind: WireTypeKind): { [name: string]: Field } {
    const numFields = this.parseUint8();
    const fields: { [name: string]: Field } = {};
    for (let i = 0; i < numFields; i++) {
      const field = this.parseField(parentKind);
      fields[field.name] = field;
    }
    return fields;
  }

  private parseField(parentKind: WireTypeKind): Field {
    const fieldName = this.parseString();
    let fieldTypeId = this.parseTypeId();
    let fieldProperties: FieldTypes;

    if (fieldTypeId === this.ArrayType || fieldTypeId === this.MapType) {
      fieldProperties = this.parseNestedType(
        fieldTypeId === this.ArrayType ? "array" : "map"
      );
    } else {
      fieldProperties = { type: "scalar" };
    }

    const attributes = this.parseAttributes();
    const constantValue =
      parentKind === WireTypeKind.Message
        ? this.parseConstantValue(WireBaseType.Byte)
        : null;

    return {
      name: fieldName,
      typeId: fieldTypeId,
      fieldProperties,
      attributes,
      constantValue,
    };
  }

  private parseNestedType(parentType: string): FieldTypes {
    if (parentType === "array") {
      const depth = this.parseUint8();
      const memberTypeId = this.parseTypeId();
      return { type: parentType, memberTypeId: memberTypeId, depth };
    }

    if (parentType === "map") {
      const keyTypeId = this.parseTypeId();
      const valueTypeId = this.parseTypeId();

      let nestedType: FieldTypes | undefined;
      if (valueTypeId === this.ArrayType || valueTypeId === this.MapType) {
        nestedType = this.parseNestedType(
          valueTypeId === this.ArrayType ? "array" : "map"
        );
      }
      return {
        type: parentType,
        keyTypeId,
        valueTypeId: valueTypeId,
        nestedType,
      };
    }

    throw new BebopRuntimeError("Invalid initial type");
  }

  private parseConstantValue(typeId: number): number | BigInt | null {
    switch (typeId) {
      case WireBaseType.Bool:
        return this.parseBool() ? 1 : 0;
      case WireBaseType.Byte:
        return this.parseUint8();
      case WireBaseType.UInt16:
        return this.parseUint16();
      case WireBaseType.Int16:
        return this.parseInt16();
      case WireBaseType.UInt32:
        return this.parseUint32();
      case WireBaseType.Int32:
        return this.parseInt32();
      case WireBaseType.UInt64:
        return BigInt(this.parseUint64());
      case WireBaseType.Int64:
        return BigInt(this.parseInt64());
      case WireBaseType.Float32:
        return this.parseFloat32();
      case WireBaseType.Float64:
        return this.parseFloat64();
      default:
        throw new BebopRuntimeError(`Unsupported constant type ID: ${typeId}`);
    }
  }

  private parseServiceDefinition(): Service {
    let name = this.parseString();
    let attributes = this.parseAttributes();
    let methods: { [name: string]: ServiceMethod } = {};
    let methodCount = this.parseUint32();
    for (let i = 0; i < methodCount; i++) {
      let methodName = this.parseString();
      let methodAttributes = this.parseAttributes();
      let methodType = this.parseUint8() as WireMethodType;
      let requestTypeId = this.parseTypeId();
      let responseTypeId = this.parseTypeId();
      let id = this.parseUint32();
      methods[methodName] = {
        name: methodName,
        attributes: methodAttributes,
        methodType: methodType,
        requestTypeId: requestTypeId,
        responseTypeId: responseTypeId,
        id: id,
      };
    }
    return {
      name: name,
      attributes: attributes,
      methods: methods,
    };
  }

  private parseString(): string {
    const start = this.pos;
    while (this.pos < this.data.length && this.data[this.pos] !== 0) {
      this.pos++;
    }
    const strBytes = this.data.subarray(start, this.pos);
    // Skip the null terminator
    if (this.pos < this.data.length) {
      this.pos++;
    }
    return decoder.decode(strBytes);
  }

  private parseUint8() {
    let value = this.view.getUint8(this.pos);
    this.pos++;
    return value;
  }

  private parseUint16() {
    let value = this.view.getUint16(this.pos, true);
    this.pos += 2;
    return value;
  }

  private parseInt16() {
    let value = this.view.getInt16(this.pos, true);
    this.pos += 2;
    return value;
  }

  private parseUint32() {
    let value = this.view.getUint32(this.pos, true);
    this.pos += 4;
    return value;
  }

  private parseInt32() {
    let value = this.view.getInt32(this.pos, true);
    this.pos += 4;
    return value;
  }

  private parseUint64() {
    let value = this.view.getBigUint64(this.pos, true);
    this.pos += 8;
    return Number(value);
  }

  private parseInt64() {
    let value = this.view.getBigInt64(this.pos, true);
    this.pos += 8;
    return Number(value);
  }

  private parseFloat32() {
    let value = this.view.getFloat32(this.pos, true);
    this.pos += 4;
    return value;
  }

  private parseFloat64() {
    let value = this.view.getFloat64(this.pos, true);
    this.pos += 8;
    return value;
  }

  private parseBool() {
    return this.parseUint8() !== 0;
  }

  private parseTypeId() {
    let typeId = this.view.getInt32(this.pos, true);
    this.pos += 4;
    return typeId;
  }
}
