import { JSON } from "assemblyscript-json/assembly";
import { Value } from "assemblyscript-json/assembly/JSON";

// @ts-ignore: decorator
@external("chord_as", "write_line")
export declare function writeLine(s: string): void;
// @ts-ignore: decorator
@external("chord_as", "write_error")
export declare function writeError(s: string): void;
// @ts-ignore: decorator
@external("chord_as", "get_bebopc_version")
export declare function getBebopcVersion(): string;

export function deserializeGeneratorContext(context: string): GeneratorContext {
  // Parse an object using the JSON object
  const json: JSON.Obj = <JSON.Obj>JSON.parse(context);
  const definitionsObj = json.getObj("definitions");
  if (definitionsObj === null) {
    throw new Error("Missing definitions");
  }
  const definitions = new Map<string, Definition>();
  definitionsObj.keys.forEach((key) => {
    const defObj = definitionsObj.getObj(key);
    if (defObj === null) {
      throw new Error(`Missing definition for ${key}`);
    }
    const def = deserializeDefinition(defObj);
    definitions.set(key, def);
  });
  const services = new Map<string, ServiceDefinition>();
  const servicesObj = json.getObj("services");
  if (servicesObj !== null) {
    servicesObj.keys.forEach((key) => {
      const serviceObj = servicesObj.getObj(key);
      if (serviceObj === null) {
        throw new Error(`Missing service for ${key}`);
      }
      const service = deserializeService(serviceObj);
      services.set(key, service);
    });
  }

  const constants = new Map<string, ConstantDefinition>();
  const constantsObj = json.getObj("constants");
  if (constantsObj !== null) {
    constantsObj.keys.forEach((key) => {
      const constantObj = constantsObj.getObj(key);
      if (constantObj === null) {
        throw new Error(`Missing constant for ${key}`);
      }
      const constant = deserializeConstant(constantObj);
      constants.set(key, constant);
    });
  }

  const config = new GeneratorConfig();
  const configObj = json.getObj("config");
  if (configObj === null) {
    throw new Error("Missing config");
  }
  const aliasRaw = configObj.getString("alias");
  if (aliasRaw !== null) {
    config.alias = aliasRaw.valueOf();
  }
  const outFileRaw = configObj.getString("outFile");
  if (outFileRaw !== null) {
    config.outFile = outFileRaw.valueOf();
  }
  const namespaceRaw = configObj.getString("namespace");
  if (namespaceRaw !== null) {
    config.namespace = namespaceRaw.valueOf();
  }
  const emitNoticeRaw = configObj.getBool("emitNotice");
  if (emitNoticeRaw !== null) {
    config.emitNotice = emitNoticeRaw.valueOf();
  }
  const emitBinarySchemaRaw = configObj.getBool("emitBinarySchema");
  if (emitBinarySchemaRaw !== null) {
    config.emitBinarySchema = emitBinarySchemaRaw.valueOf();
  }
  const servicesRaw = configObj.getString("services");
  if (servicesRaw !== null) {
    config.services = deserializeServicesType(servicesRaw.valueOf());
  }

  config.options = new Map<string, string>();
  const optionsObj = configObj.getObj("options");
  if (optionsObj !== null) {
    optionsObj.keys.forEach((key) => {
      const option = optionsObj.getString(key);
      if (option === null) {
        throw new Error(`Missing option for ${key}`);
      }
      config.options.set(key, option.valueOf());
    });
  }
  const generatorContext = new GeneratorContext();
  generatorContext.definitions = definitions;
  generatorContext.services = services;
  generatorContext.constants = constants;
  generatorContext.config = config;

  return generatorContext;
}

function deserializeServicesType(str: string): TempoServices {
  switch (str) {
    case "none":
      return TempoServices.None;
    case "both":
      return TempoServices.Both;
    case "client":
      return TempoServices.Client;
    case "server":
      return TempoServices.Server;
    default:
      throw new Error(`Unknown services type: ${str}`);
  }
}
function deserializeDefinition(obj: JSON.Obj): Definition {
  const kind = obj.getString("kind");
  if (kind === null) {
    throw new Error("Missing kind");
  }

  switch (kind.toString()) {
    case "struct":
      return deserializeStruct(obj);
    case "message":
      return deserializeMessage(obj);
    case "union":
      return deserializeUnion(obj);
    case "enum":
      return deserializeEnum(obj);
    case "service":
      return deserializeService(obj);
    case "constant":
      return deserializeConstant(obj);
    default:
      throw new Error(`Unknown kind: ${kind}`);
  }
}

function deserializeConstant(obj: JSON.Obj): ConstantDefinition {
  const constant = new ConstantDefinition();
  constant.kind = "constant";
  const docsRaw = obj.getString("documentation");
  if (docsRaw !== null) {
    constant.documentation = docsRaw.valueOf();
  }
  const typeRaw = obj.getString("type");
  if (typeRaw !== null) {
    constant.type = new BaseType(typeRaw.valueOf());
  }

  if (constant.type.name === "bool") {
    const valueRaw = obj.getBool("value");
    if (valueRaw !== null) {
      constant.value = new ValueContainer(
        valueRaw.valueOf().toString(),
        constant.type,
      );
    }
  } else {
    const valueRaw = obj.getString("value");
    if (valueRaw !== null) {
      constant.value = new ValueContainer(valueRaw.valueOf(), constant.type);
    }
  }
  return constant;
}

function deserializeService(obj: JSON.Obj): ServiceDefinition {
  const service = new ServiceDefinition();
  service.kind = "service";
  const docsRaw = obj.getString("documentation");
  if (docsRaw !== null) {
    service.documentation = docsRaw.valueOf();
  }
  service.decorators = deserializeDecorators(obj);
  service.methods = deserializeMethods(obj);
  return service;
}

function deserializeMethods(obj: JSON.Obj): Map<string, Method> {
  const methods = new Map<string, Method>();
  const methodsObj = obj.getObj("methods");
  if (methodsObj !== null) {
    methodsObj.keys.forEach((key) => {
      const methodObj = methodsObj.getObj(key);
      if (methodObj === null) {
        throw new Error(`Missing method for ${key}`);
      }
      const method = deserializeMethod(methodObj);
      methods.set(key, method);
    });
  }
  return methods;
}

function deserializeMethod(obj: JSON.Obj): Method {
  const method = new Method();
  const docsRaw = obj.getString("documentation");
  if (docsRaw !== null) {
    method.documentation = docsRaw.valueOf();
  }
  method.decorators = deserializeDecorators(obj);
  method.type = deserializeMethodType(obj);
  const requestTypeRaw = obj.getString("requestType");
  if (requestTypeRaw !== null) {
    method.requestType = requestTypeRaw.valueOf();
  }
  const responseTypeRaw = obj.getString("responseType");
  if (responseTypeRaw !== null) {
    method.responseType = responseTypeRaw.valueOf();
  }
  const idRaw = obj.getInteger("id");
  if (idRaw !== null) {
    method.id = idRaw.valueOf();
  }
  return method;
}

function deserializeMethodType(obj: JSON.Obj): MethodType {
  const type = obj.getString("type");
  if (type === null) {
    throw new Error("Missing method type");
  }
  switch (type.toString().toLowerCase()) {
    case "unary":
      return MethodType.Unary;
    case "serverstream":
      return MethodType.ServerStream;
    case "clientstream":
      return MethodType.ClientStream;
    case "duplexstream":
      return MethodType.DuplexStream;
    default:
      throw new Error(`Unknown method type: ${type}`);
  }
}

function deserializeEnum(obj: JSON.Obj): EnumDefinition {
  const enumDef = new EnumDefinition();
  enumDef.kind = "enum";
  const docsRaw = obj.getString("documentation");
  if (docsRaw !== null) {
    enumDef.documentation = docsRaw.valueOf();
  }
  enumDef.decorators = deserializeDecorators(obj);
  const baseTypeRaw = obj.getString("baseType");
  if (baseTypeRaw !== null) {
    enumDef.baseType = new BaseType(baseTypeRaw.valueOf());
  }
  const isBitFlagsRaw = obj.getBool("isBitFlags");
  if (isBitFlagsRaw !== null) {
    enumDef.isBitFlags = isBitFlagsRaw.valueOf();
  }
  const minimalEncodedSizeRaw = obj.getInteger("minimalEncodedSize");
  if (minimalEncodedSizeRaw !== null) {
    enumDef.minimalEncodedSize = minimalEncodedSizeRaw.valueOf();
  }

  enumDef.members = deserializeEnumMembers(obj, enumDef.baseType);
  return enumDef;
}

function deserializeEnumMembers(
  obj: JSON.Obj,
  baseType: BaseType,
): Map<string, EnumMember> {
  const members = new Map<string, EnumMember>();
  const membersObj = obj.getObj("members");
  if (membersObj !== null) {
    membersObj.keys.forEach((key) => {
      const memberObj = membersObj.getObj(key);
      if (memberObj === null) {
        throw new Error(`Missing member for ${key}`);
      }
      const member = deserializeEnumMember(memberObj, baseType);
      members.set(key, member);
    });
  }
  return members;
}

function deserializeEnumMember(obj: JSON.Obj, baseType: BaseType): EnumMember {
  const member = new EnumMember();
  const docsRaw = obj.getString("documentation");
  if (docsRaw !== null) {
    member.documentation = docsRaw.valueOf();
  }
  const constantRaw = obj.getInteger("constant");
  if (constantRaw !== null) {
    member.constant = constantRaw.valueOf();
  }
  member.decorators = deserializeDecorators(obj);
  return member;
}

function deserializeUnion(obj: JSON.Obj): UnionDefinition {
  const union = new UnionDefinition();
  union.kind = "union";
  const docsRaw = obj.getString("documentation");
  if (docsRaw !== null) {
    union.documentation = docsRaw.valueOf();
  }
  union.decorators = deserializeDecorators(obj);
  const minimalEncodedSizeRaw = obj.getInteger("minimalEncodedSize");
  if (minimalEncodedSizeRaw !== null) {
    union.minimalEncodedSize = minimalEncodedSizeRaw.valueOf();
  }
  const discriminatorInParentRaw = obj.getInteger("discriminatorInParent");
  if (discriminatorInParentRaw !== null) {
    union.discriminatorInParent = discriminatorInParentRaw.valueOf();
  }
  union.branches = deserializeBranches(obj);
  return union;
}

function deserializeBranches(obj: JSON.Obj): Map<number, string> {
  const branches = new Map<number, string>();
  const branchesObj = obj.getObj("branches");
  if (branchesObj !== null) {
    branchesObj.keys.forEach((key) => {
      const branchObj = branchesObj.getObj(key);
      if (branchObj === null) {
        throw new Error(`Missing branch for ${key}`);
      }
      const discriminatorRaw = branchObj.getInteger(key);
      if (discriminatorRaw === null) {
        throw new Error(`Missing discriminator for ${key}`);
      }

      branches.set(discriminatorRaw.valueOf(), key);
    });
  }
  return branches;
}

function deserializeStruct(obj: JSON.Obj): StructDefinition {
  const struct = new StructDefinition();
  struct.kind = "struct";
  const docsRaw = obj.getString("documentation");
  if (docsRaw !== null) {
    struct.documentation = docsRaw.valueOf();
  }

  struct.decorators = deserializeDecorators(obj);
  const isReadOnlyRaw = obj.getBool("isReadOnly");
  if (isReadOnlyRaw !== null) {
    struct.isReadOnly = isReadOnlyRaw.valueOf();
  }
  const isFixedSizeRaw = obj.getBool("isFixedSize");
  if (isFixedSizeRaw !== null) {
    struct.isFixedSize = isFixedSizeRaw.valueOf();
  }

  const minimalEncodedSizeRaw = obj.getInteger("minimalEncodedSize");
  if (minimalEncodedSizeRaw !== null) {
    struct.minimalEncodedSize = minimalEncodedSizeRaw.valueOf();
  }
  const discriminatorInParentRaw = obj.getInteger("discriminatorInParent");
  if (discriminatorInParentRaw !== null) {
    struct.discriminatorInParent = discriminatorInParentRaw.valueOf();
  }
  struct.fields = deserializeFields(obj);
  return struct;
}

function deserializeMessage(obj: JSON.Obj): MessageDefinition {
  const message = new MessageDefinition();
  message.kind = "message";
  const docsRaw = obj.getString("documentation");
  if (docsRaw !== null) {
    message.documentation = docsRaw.valueOf();
  }
  message.decorators = deserializeDecorators(obj);
  const minimalEncodedSizeRaw = obj.getInteger("minimalEncodedSize");
  if (minimalEncodedSizeRaw !== null) {
    message.minimalEncodedSize = minimalEncodedSizeRaw.valueOf();
  }
  const discriminatorInParentRaw = obj.getInteger("discriminatorInParent");
  if (discriminatorInParentRaw !== null) {
    message.discriminatorInParent = discriminatorInParentRaw.valueOf();
  }

  message.fields = deserializeFields(obj);
  return message;
}

function deserializeFields(obj: JSON.Obj): Map<string, Field> {
  const fields = new Map<string, Field>();
  const fieldsObj = obj.getObj("fields");
  if (fieldsObj !== null) {
    fieldsObj.keys.forEach((key) => {
      const fieldObj = fieldsObj.getObj(key);
      if (fieldObj === null) {
        throw new Error(`Missing field for ${key}`);
      }
      const field = deserializeField(fieldObj);
      fields.set(key, field);
    });
  }
  return fields;
}

function deserializeField(obj: JSON.Obj): Field {
  const field = new Field();
  const docsRaw = obj.getString("documentation");
  if (docsRaw !== null) {
    field.documentation = docsRaw.valueOf();
  }
  field.decorators = deserializeDecorators(obj);
  field.type = deserializeFieldType(obj);
  const indexRaw = obj.getInteger("index");
  if (indexRaw !== null) {
    field.index = indexRaw.valueOf();
  }
  return field;
}

function deserializeFieldType(obj: JSON.Obj): FieldType {
  const type = obj.getString("type");
  if (type === null) {
    throw new Error("Missing field type");
  }
  switch (type.toString()) {
    case "array":
      const arrayObj = obj.getObj("array");
      if (arrayObj === null) {
        throw new Error("Missing array type");
      }
      return deserializeArrayType(arrayObj);
    case "map":
      const mapObj = obj.getObj("map");
      if (mapObj === null) {
        throw new Error("Missing map type");
      }
      return deserializeMapType(mapObj);
    default:
      return new BaseType(type.valueOf());
  }
}

function deserializeArrayType(obj: JSON.Obj): ArrayType {
  const depthObj = obj.getNum("depth");
  if (depthObj === null) {
    throw new Error("Missing array depth");
  }
  const depth = depthObj.valueOf();
  const memberTypeObj = obj.getString("memberType");
  if (memberTypeObj === null) {
    throw new Error("Missing array member type");
  }
  return new ArrayType(depth, new BaseType(memberTypeObj.valueOf()));
}

function deserializeMapType(obj: JSON.Obj): MapType {
  const keyTypeStr = obj.getString("keyType");
  if (keyTypeStr === null) {
    throw new Error("Missing map key type");
  }
  const valueTypeObj = obj.getString("valueType");
  if (valueTypeObj === null) {
    throw new Error("Missing map value type");
  }
  const keyType = keyTypeStr.valueOf();
  const valueType = valueTypeObj.valueOf();
  if (valueType === "array") {
    const arrayTypeObj = obj.getObj("array");
    if (arrayTypeObj === null) {
      throw new Error("Missing array type");
    }
    return new MapType(
      new BaseType(keyType),
      deserializeArrayType(arrayTypeObj),
    );
  } else if (valueType === "map") {
    const mapTypeObj = obj.getObj("map");
    if (mapTypeObj === null) {
      throw new Error("Missing map type");
    }
    return new MapType(new BaseType(keyType), deserializeMapType(mapTypeObj));
  }
  return new MapType(new BaseType(keyType), new BaseType(valueType));
}

function deserializeDecorators(obj: JSON.Obj): Map<string, Decorator> {
  const decorators = new Map<string, Decorator>();
  const decoratorsObj = obj.getObj("decorators");
  if (decoratorsObj !== null) {
    decoratorsObj.keys.forEach((key) => {
      const decoratorObj = decoratorsObj.getObj(key);
      if (decoratorObj === null) {
        throw new Error(`Missing decorator for ${key}`);
      }
      const decorator = deserializeDecorator(decoratorObj);
      decorators.set(key, decorator);
    });
  }
  return decorators;
}

function deserializeDecorator(obj: JSON.Obj): Decorator {
  const decorator = new Decorator();
  const argsObj = obj.getObj("arguments");
  if (argsObj !== null) {
    const args = new Map<string, DecoratorArgument>();
    argsObj.keys.forEach((key) => {
      const argObj = argsObj.getObj(key);
      if (argObj === null) {
        throw new Error(`Missing decorator argument for ${key}`);
      }
      const arg = deserializeDecoratorArgument(argObj);
      args.set(key, arg);
    });
    decorator.arguments = args;
  }
  return decorator;
}

function deserializeDecoratorArgument(obj: JSON.Obj): DecoratorArgument {
  const arg = new DecoratorArgument();
  const type = obj.getString("type");
  if (type === null) {
    throw new Error("Missing decorator argument type");
  }
  arg.type = new BaseType(type.valueOf());
  const value = obj.getString("value");
  if (value === null) {
    throw new Error("Missing decorator argument value");
  }
  arg.value = new ValueContainer(value.valueOf(), arg.type);
  return arg;
}

class GeneratorContext {
  definitions!: Map<string, Definition>;
  services!: Map<string, ServiceDefinition>;
  constants!: Map<string, ConstantDefinition>;
  config!: GeneratorConfig;
}

const baseTypes = [
  "byte",
  "uint8",
  "int16",
  "uint16",
  "int32",
  "uint32",
  "int64",
  "uint64",
  "float32",
  "float64",
  "bool",
  "string",
  "guid",
  "date",
];

class BaseType {
  name: string;
  constructor(name: string) {
    if (!baseTypes.includes(name)) {
      throw new Error(`Invalid base type: ${name}`);
    }
    this.name = name;
  }
}

class ValueContainer {
  value: string;
  type: BaseType;
  constructor(value: string, type: BaseType) {
    this.value = value;
    this.type = type;
  }
}

class ArrayType {
  depth: number;
  memberType: FieldType;
  constructor(depth: number, memberType: FieldType) {
    this.depth = depth;
    this.memberType = memberType;
  }
}

class MapType {
  keyType: BaseType;
  valueType: FieldType;

  constructor(keyType: BaseType, valueType: FieldType) {
    this.keyType = keyType;
    this.valueType = valueType;
  }
}

type FieldType = BaseType | ArrayType | MapType;

class Decorator {
  arguments!: Map<string, DecoratorArgument>;
}

class DecoratorArgument {
  type!: BaseType;
  value!: ValueContainer;
}

class Definition {
  kind!: string;
  documentation!: string;
  decorators!: Map<string, Decorator>;
  parent!: string;
}

class RecordDefinition extends Definition {
  minimalEncodedSize!: number;
  discriminatorInParent!: number;
}

class FieldsDefinition extends RecordDefinition {
  fields!: Map<string, Field>;
}

class StructDefinition extends FieldsDefinition {
  isReadOnly!: bool;
  isFixedSize!: bool;
}

class MessageDefinition extends FieldsDefinition {
  
}

class Field {
  documentation!: string;
  decorators!: Map<string, Decorator>;
  type!: FieldType;
  index!: number;
}

class UnionDefinition extends RecordDefinition {
  branches!: Map<number, string>;
}

class EnumDefinition extends Definition {
  isBitFlags!: bool;
  minimalEncodedSize!: number;
  baseType!: BaseType;
  members!: Map<string, EnumMember>;
}

class EnumMember {
  constant!: number;
  documentation!: string;
  decorators!: Map<string, Decorator>;
}

class ServiceDefinition extends Definition {
  methods!: Map<string, Method>;
}

class Method {
  documentation!: string;
  decorators!: Map<string, Decorator>;
  type!: MethodType;
  requestType!: string;
  responseType!: string;
  id!: number;
}

enum MethodType {
  Unary,
  ServerStream,
  ClientStream,
  DuplexStream,
}

class ConstantDefinition extends Definition {
  type!: BaseType;
  value!: ValueContainer;
}

class GeneratorConfig {
  alias!: string;
  outFile!: string;
  namespace!: string;
  emitNotice!: bool;
  emitBinarySchema!: bool;
  services!: TempoServices;
  options!: Map<string, string>;
}

enum TempoServices {
  None,
  Both,
  Client,
  Server,
}
