import {
  BebopTypeGuard,
  BebopRuntimeError,
  BebopJson,
  GuidMap,
  Guid,
} from "./index";

import { describe, expect, it } from "vitest";

describe("BebopJson", () => {
  describe("replacer", () => {
    it("should support BigInt values", () => {
      const obj = { bigInt: BigInt(Number.MAX_SAFE_INTEGER) + 1n };
      const jsonString = JSON.stringify(obj, BebopJson.replacer);
      const parsedObj = JSON.parse(jsonString, BebopJson.reviver);
      expect(parsedObj.bigInt).toEqual(obj.bigInt);
    });

    it("should support Map values", () => {
      const obj = { map: new Map([["key", "value"]]) };
      const jsonString = JSON.stringify(obj, BebopJson.replacer);
      const parsedObj = JSON.parse(jsonString, BebopJson.reviver);
      expect(parsedObj.map.get("key")).toEqual("value");
    });

    it("should support GuidMap", () => {
      const guid = Guid.newGuid();
      const obj = { map: new GuidMap([[guid, "value"]]) };
      const jsonString = JSON.stringify(obj, BebopJson.replacer);
      const parsedObj = JSON.parse(jsonString, BebopJson.reviver) as typeof obj;
      expect(parsedObj.map.get(guid)).toEqual("value");
    });

    it("should support Uint8Array values", () => {
      const obj = { uint8: new Uint8Array([0, 1, 2, 3]) };
      const jsonString = JSON.stringify(obj, BebopJson.replacer);
      const parsedObj = JSON.parse(jsonString, BebopJson.reviver);
      expect(parsedObj.uint8).toEqual(obj.uint8);
    });

    it("should support a complex object with multiple types", () => {
      const guid = Guid.newGuid();
      const obj = {
        bigInt: BigInt(Number.MAX_SAFE_INTEGER) + 1n,
        nestedObj: {
          bigInt: BigInt(Number.MAX_SAFE_INTEGER) + 1n,
        },
        map: new Map([["key", "value"]]),
        nestedMap: new Map([["key", new GuidMap([[guid, "value"]])]]),
        date: new Date("2022-01-01T00:00:00.000Z"),
        uint8: new Uint8Array([0, 1, 2, 3]),
      };
      const jsonString = JSON.stringify(obj, BebopJson.replacer, 4);
      const parsedObj = JSON.parse(jsonString, BebopJson.reviver) as typeof obj;
      expect(parsedObj.bigInt).toEqual(obj.bigInt);
      expect(parsedObj.map.get("key")).toEqual("value");
      expect(parsedObj.nestedMap.get("key")).toBeInstanceOf(GuidMap);
      expect(parsedObj.nestedMap.get("key")?.get(guid)).toEqual("value");
      expect(parsedObj.date.getTime()).toEqual(obj.date.getTime());
      expect(parsedObj.uint8).toEqual(obj.uint8);
    });
  });

  describe("keysExist", () => {
    it("should return an object with boolean values indicating whether each key path exists in the object", () => {
      const obj = { a: { b: { c: 1 } }, d: 2 };
      const result = BebopJson.keysExist(["a.b.c", "d", "e"], obj);
      expect(result).toEqual({ "a.b.c": true, d: true, e: false });
    });
  });

  describe("ensureKeysExist", () => {
    it("should throw an error if a required key path does not exist in the parsed object", () => {
      const obj = { a: { b: { c: 1 } }, d: 2 };
      expect(() =>
        BebopJson.ensureKeysExist(["a.b.c", "d", "e"], obj)
      ).toThrow();
      expect(() => BebopJson.ensureKeysExist(["a.b.c", "e"], obj)).toThrow();
    });
  });

  describe("security checks", () => {
    it("should not be vulnerable to prototype pollution", () => {
      const json = '{"user":{"__proto__":{"admin": true}}}';
      expect(() => JSON.parse(json, BebopJson.reviver)).toThrow(BebopRuntimeError);
    });
  });
});

describe("BebopTypeGuard", () => {
  describe("ensureBoolean", () => {
    it("should not throw an error for a valid boolean", () => {
      expect(() => BebopTypeGuard.ensureBoolean(true)).not.toThrow();
      expect(() => BebopTypeGuard.ensureBoolean(false)).not.toThrow();
    });

    it("should throw an error for an invalid boolean", () => {
      expect(() => BebopTypeGuard.ensureBoolean(null)).toThrow(
        BebopRuntimeError
      );
      expect(() => BebopTypeGuard.ensureBoolean(undefined)).toThrow(
        BebopRuntimeError
      );
      expect(() => BebopTypeGuard.ensureBoolean("true")).toThrow(
        BebopRuntimeError
      );
      expect(() => BebopTypeGuard.ensureBoolean(1)).toThrow(BebopRuntimeError);
    });
  });

  describe("ensureUint8", () => {
    it("should not throw an error for a valid Uint8 number", () => {
      expect(() => BebopTypeGuard.ensureUint8(0)).not.toThrow();
      expect(() => BebopTypeGuard.ensureUint8(255)).not.toThrow();
    });

    it("should throw an error for an invalid Uint8 number", () => {
      expect(() => BebopTypeGuard.ensureUint8(null)).toThrow(BebopRuntimeError);
      expect(() => BebopTypeGuard.ensureUint8(undefined)).toThrow(
        BebopRuntimeError
      );
      expect(() => BebopTypeGuard.ensureUint8(-1)).toThrow(BebopRuntimeError);
      expect(() => BebopTypeGuard.ensureUint8(256)).toThrow(BebopRuntimeError);
      expect(() => BebopTypeGuard.ensureUint8(1.5)).toThrow(BebopRuntimeError);
    });
  });

  describe("ensureUint16", () => {
    it("should not throw an error for a valid Uint16 number", () => {
      expect(() => BebopTypeGuard.ensureUint16(0)).not.toThrow();
      expect(() => BebopTypeGuard.ensureUint16(65535)).not.toThrow();
    });

    it("should throw an error for an invalid Uint16 number", () => {
      expect(() => BebopTypeGuard.ensureUint16(null)).toThrow(
        BebopRuntimeError
      );
      expect(() => BebopTypeGuard.ensureUint16(undefined)).toThrow(
        BebopRuntimeError
      );
      expect(() => BebopTypeGuard.ensureUint16(-1)).toThrow(BebopRuntimeError);
      expect(() => BebopTypeGuard.ensureUint16(65536)).toThrow(
        BebopRuntimeError
      );
      expect(() => BebopTypeGuard.ensureUint16(1.5)).toThrow(BebopRuntimeError);
    });
  });

  describe("ensureInt16", () => {
    it("should not throw an error for a valid Int16 number", () => {
      expect(() => BebopTypeGuard.ensureInt16(-32768)).not.toThrow();
      expect(() => BebopTypeGuard.ensureInt16(32767)).not.toThrow();
    });

    it("should throw an error for an invalid Int16 number", () => {
      expect(() => BebopTypeGuard.ensureInt16(null)).toThrow(BebopRuntimeError);
      expect(() => BebopTypeGuard.ensureInt16(undefined)).toThrow(
        BebopRuntimeError
      );
      expect(() => BebopTypeGuard.ensureInt16(-32769)).toThrow(
        BebopRuntimeError
      );
      expect(() => BebopTypeGuard.ensureInt16(32768)).toThrow(
        BebopRuntimeError
      );
      expect(() => BebopTypeGuard.ensureInt16(1.5)).toThrow(BebopRuntimeError);
    });
  });

  describe("ensureUint32", () => {
    it("should not throw an error for a valid Uint32 number", () => {
      expect(() => BebopTypeGuard.ensureUint32(0)).not.toThrow();
      expect(() => BebopTypeGuard.ensureUint32(4294967295)).not.toThrow();
    });

    it("should throw an error for an invalid Uint32 number", () => {
      expect(() => BebopTypeGuard.ensureUint32(null)).toThrow(
        BebopRuntimeError
      );
      expect(() => BebopTypeGuard.ensureUint32(undefined)).toThrow(
        BebopRuntimeError
      );
      expect(() => BebopTypeGuard.ensureUint32(-1)).toThrow(BebopRuntimeError);
      expect(() => BebopTypeGuard.ensureUint32(4294967296)).toThrow(
        BebopRuntimeError
      );
      expect(() => BebopTypeGuard.ensureUint32(1.5)).toThrow(BebopRuntimeError);
    });
  });

  describe("ensureInt32", () => {
    it("should not throw an error for a valid Int32 number", () => {
      expect(() => BebopTypeGuard.ensureInt32(-2147483648)).not.toThrow();
      expect(() => BebopTypeGuard.ensureInt32(2147483647)).not.toThrow();
    });

    it("should throw an error for an invalid Int32 number", () => {
      expect(() => BebopTypeGuard.ensureInt32(null)).toThrow(BebopRuntimeError);
      expect(() => BebopTypeGuard.ensureInt32(undefined)).toThrow(
        BebopRuntimeError
      );
      expect(() => BebopTypeGuard.ensureInt32(-2147483649)).toThrow(
        BebopRuntimeError
      );
      expect(() => BebopTypeGuard.ensureInt32(2147483648)).toThrow(
        BebopRuntimeError
      );
      expect(() => BebopTypeGuard.ensureInt32(1.5)).toThrow(BebopRuntimeError);
    });
  });

  describe("ensureFloat", () => {
    it("should not throw an error for a valid Float number", () => {
      expect(() => BebopTypeGuard.ensureFloat(-3.4028235e38)).not.toThrow();
      expect(() => BebopTypeGuard.ensureFloat(3.4028235e38)).not.toThrow();
    });

    it("should throw an error for an invalid Float32 number", () => {
      expect(() => BebopTypeGuard.ensureFloat(null)).toThrow(BebopRuntimeError);
      expect(() => BebopTypeGuard.ensureFloat(undefined)).toThrow(
        BebopRuntimeError
      );
      expect(() => BebopTypeGuard.ensureFloat("1.5")).toThrow(
        BebopRuntimeError
      );
    });
  });

  describe("ensureString", () => {
    it("should not throw an error for a valid string", () => {
      expect(() => BebopTypeGuard.ensureString("hello")).not.toThrow();
      expect(() => BebopTypeGuard.ensureString("")).not.toThrow();
    });

    it("should throw an error for an invalid string", () => {
      expect(() => BebopTypeGuard.ensureString(null)).toThrow(
        BebopRuntimeError
      );
      expect(() => BebopTypeGuard.ensureString(undefined)).toThrow(
        BebopRuntimeError
      );
      expect(() => BebopTypeGuard.ensureString(1)).toThrow(BebopRuntimeError);
      expect(() => BebopTypeGuard.ensureString({})).toThrow(BebopRuntimeError);
    });
  });

  describe("ensureArray", () => {
    const validator = (value: any) => {
      if (typeof value !== "number") {
        throw new BebopRuntimeError("Invalid type");
      }
    };
    it("should not throw an error for a valid array", () => {
      expect(() => BebopTypeGuard.ensureArray([], validator)).not.toThrow();
      expect(() =>
        BebopTypeGuard.ensureArray([1, 2, 3], validator)
      ).not.toThrow();
    });

    it("should throw an error for an invalid array", () => {
      expect(() => BebopTypeGuard.ensureArray(null, validator)).toThrow(
        BebopRuntimeError
      );
      expect(() => BebopTypeGuard.ensureArray(undefined, validator)).toThrow(
        BebopRuntimeError
      );
      expect(() => BebopTypeGuard.ensureArray(1, validator)).toThrow(
        BebopRuntimeError
      );
      expect(() => BebopTypeGuard.ensureArray({}, validator)).toThrow(
        BebopRuntimeError
      );
    });
  });

  describe("ensureMap", () => {
    it("should not throw an error for a valid map", () => {
      const keyTypeValidator = (value: any) => {
        if (typeof value !== "number") {
          throw new BebopRuntimeError("Invalid type");
        }
      };
      const valueTypeValidator = (value: any) => {
        if (typeof value !== "string") {
          throw new BebopRuntimeError("Invalid type");
        }
      };
      const map = new Map<number, string>();
      map.set(1, "one");
      map.set(2, "two");

      expect(() =>
        BebopTypeGuard.ensureMap(map, keyTypeValidator, valueTypeValidator)
      ).not.toThrow();
    });
    it("should throw an error for an invalid map", () => {
      const keyTypeValidator = (value: any) => {
        if (typeof value !== "number") {
          throw new BebopRuntimeError("Invalid type");
        }
      };
      const valueTypeValidator = (value: any) => {
        if (typeof value !== "string") {
          throw new BebopRuntimeError("Invalid type");
        }
      };
      const map = new Map<number, any>();
      map.set(1, "one");
      map.set(2, 3);
      expect(() =>
        BebopTypeGuard.ensureMap(map, keyTypeValidator, valueTypeValidator)
      ).toThrow(BebopRuntimeError);
      expect(() =>
        BebopTypeGuard.ensureMap(null, keyTypeValidator, valueTypeValidator)
      ).toThrow(BebopRuntimeError);
      expect(() =>
        BebopTypeGuard.ensureMap(
          undefined,
          keyTypeValidator,
          valueTypeValidator
        )
      ).toThrow(BebopRuntimeError);
      expect(() =>
        BebopTypeGuard.ensureMap(1, keyTypeValidator, valueTypeValidator)
      ).toThrow(BebopRuntimeError);
      expect(() =>
        BebopTypeGuard.ensureMap({}, keyTypeValidator, valueTypeValidator)
      ).toThrow(BebopRuntimeError);
    });
  });
});
