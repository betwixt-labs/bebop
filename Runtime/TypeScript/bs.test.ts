import { BebopRuntimeError, BinarySchema, Guid, BebopJson } from "./index";

import { describe, expect, it } from "vitest";

const schemaData = new Uint8Array([
  3, 5, 0, 0, 0, 82, 105, 103, 104, 116, 115, 0, 4, 1, 102, 108, 97, 103, 115,
  0, 0, 254, 255, 255, 255, 1, 1, 0, 0, 0, 4, 68, 101, 102, 97, 117, 108, 116,
  0, 0, 0, 85, 115, 101, 114, 0, 0, 1, 77, 111, 100, 0, 0, 2, 65, 100, 109, 105,
  110, 0, 0, 3, 65, 112, 105, 82, 101, 113, 117, 101, 115, 116, 0, 1, 0, 0, 12,
  0, 0, 0, 0, 2, 117, 114, 105, 0, 245, 255, 255, 255, 0, 116, 114, 97, 99, 101,
  0, 249, 255, 255, 255, 0, 68, 101, 102, 97, 117, 108, 116, 82, 101, 115, 112,
  111, 110, 115, 101, 0, 1, 0, 0, 5, 0, 0, 0, 0, 2, 114, 105, 103, 104, 116,
  115, 0, 0, 0, 0, 0, 0, 117, 115, 101, 114, 110, 97, 109, 101, 0, 245, 255,
  255, 255, 0, 69, 114, 114, 111, 114, 82, 101, 115, 112, 111, 110, 115, 101, 0,
  2, 0, 5, 0, 0, 0, 3, 114, 101, 97, 115, 111, 110, 0, 245, 255, 255, 255, 0, 1,
  99, 111, 100, 101, 0, 251, 255, 255, 255, 0, 2, 105, 100, 0, 244, 255, 255,
  255, 1, 100, 101, 112, 114, 101, 99, 97, 116, 101, 100, 0, 1, 114, 101, 97,
  115, 111, 110, 0, 245, 255, 255, 255, 106, 117, 115, 116, 32, 98, 101, 99, 97,
  117, 115, 101, 0, 3, 65, 112, 105, 82, 101, 115, 112, 111, 110, 115, 101, 0,
  3, 0, 10, 0, 0, 0, 2, 1, 2, 0, 0, 0, 2, 3, 0, 0, 0, 1, 0, 0, 0, 71, 114, 101,
  101, 116, 101, 114, 0, 0, 1, 0, 0, 0, 115, 97, 121, 72, 101, 108, 108, 111, 0,
  0, 0, 1, 0, 0, 0, 4, 0, 0, 0, 85, 246, 254, 77,
]);

const recordData = new Uint8Array([
  9, 0, 0, 0, 1, 3, 4, 0, 0, 0, 116, 101, 115, 116,
]);

const recordData2 = new Uint8Array([
  39, 0, 0, 0, 2, 35, 0, 0, 0, 1, 7, 0, 0, 0, 102, 97, 105, 108, 117, 114, 101,
  2, 244, 1, 0, 0, 3, 13, 118, 226, 151, 228, 248, 79, 67, 167, 238, 53, 223,
  133, 176, 67, 209, 0,
]);

const schema = new BinarySchema(schemaData);

describe("binary schema", () => {
  it("can read", () => {
    schema.get();
    const ast = schema.ast;
    console.log(JSON.stringify(ast, BebopJson.replacer, 2));

    expect(ast).toBeDefined();
    expect(ast.bebopVersion).toBe(3);
    expect(ast.definitions).keys([
      "Rights",
      "ApiRequest",
      "ApiResponse",
      "DefaultResponse",
      "ErrorResponse",
    ]);
    expect(ast.definitions["ApiResponse"]).toBeDefined();
    expect(ast.definitions["ApiResponse"].kind).toBe(3);
    expect(ast.definitions["Rights"]["members"]).keys([
      "Default",
      "User",
      "Mod",
      "Admin",
    ]);
  });

  it("can read a record using the schema", () => {
    const record = schema.reader.read("ApiResponse", recordData);
    expect(record).toBeDefined();
    expect(record["discriminator"]).toBe(1);
    expect(record["value"]).keys(["rights", "username"]);
    const value = record["value"] as Record<string, unknown>;
    expect(value["rights"]).toBe(3);
    expect(value["username"]).toBe("test");
  });

  it("can read a record with guid using the schema", () => {
    const record = schema.reader.read("ApiResponse", recordData2);
    expect(record).toBeDefined();
    expect(record["discriminator"]).toBe(2);
    expect(record["value"]).keys(["code", "reason", "id"]);
    const value = record["value"] as Record<string, unknown>;
    expect(value["code"]).toBe(500);
    expect(value["reason"]).toBe("failure");
    expect(value["id"]).toStrictEqual(
      Guid.parseGuid("97e2760d-f8e4-434f-a7ee-35df85b043d1")
    );
  });

  it("can write a record using the schema", () => {
    const record = schema.reader.read("ApiResponse", recordData);
    expect(record).toBeDefined();
    const data = schema.writer.write("ApiResponse", record);
    expect(data).toBeDefined();
    expect(data).toEqual(recordData);
    const read = schema.reader.read("ApiResponse", data);
    expect(read).toEqual(record);
  });
  it("cannot modify schema data", () => {
    expect(() => {
      schema.raw[0] = 0;
    }).toThrow(BebopRuntimeError);
  });
});
