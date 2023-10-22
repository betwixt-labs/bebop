import { BebopRuntimeError, BinarySchema, Guid } from "./index";

import { describe, expect, it } from "vitest";

const schemaData = new Uint8Array([
  2, 4, 0, 0, 0, 83, 116, 97, 116, 117, 115, 0, 4, 0, 254, 255, 255, 255, 2, 79,
  107, 97, 121, 0, 0, 1, 68, 111, 97, 98, 108, 101, 0, 0, 2, 80, 111, 105, 110,
  116, 0, 1, 0, 1, 6, 120, 0, 250, 255, 255, 255, 0, 121, 0, 250, 255, 255, 255,
  0, 110, 97, 109, 101, 0, 245, 255, 255, 255, 0, 115, 116, 97, 116, 117, 115,
  0, 0, 0, 0, 0, 0, 110, 117, 109, 98, 101, 114, 115, 0, 242, 255, 255, 255, 0,
  250, 255, 255, 255, 0, 97, 77, 97, 112, 0, 241, 255, 255, 255, 245, 255, 255,
  255, 245, 255, 255, 255, 0, 67, 111, 111, 114, 100, 105, 110, 97, 116, 101, 0,
  2, 0, 3, 112, 111, 105, 110, 116, 0, 1, 0, 0, 0, 0, 1, 105, 100, 0, 244, 255,
  255, 255, 0, 2, 110, 101, 115, 116, 101, 100, 0, 241, 255, 255, 255, 245, 255,
  255, 255, 241, 255, 255, 255, 245, 255, 255, 255, 242, 255, 255, 255, 1, 245,
  255, 255, 255, 0, 3, 80, 111, 119, 101, 114, 0, 3, 0, 2, 1, 1, 0, 0, 0, 2, 2,
  0, 0, 0, 0, 0, 0, 0,
]);

const recordData = new Uint8Array([
  160, 0, 0, 0, 2, 156, 0, 0, 0, 1, 232, 3, 0, 0, 208, 7, 0, 0, 11, 0, 0, 0, 72,
  101, 108, 108, 111, 32, 87, 111, 114, 108, 100, 2, 2, 0, 0, 0, 0, 8, 0, 0,
  192, 33, 0, 0, 1, 0, 0, 0, 5, 0, 0, 0, 104, 101, 108, 108, 111, 5, 0, 0, 0,
  119, 111, 114, 108, 100, 2, 207, 129, 243, 159, 71, 35, 167, 74, 162, 56, 120,
  38, 2, 125, 63, 63, 3, 1, 0, 0, 0, 9, 0, 0, 0, 111, 117, 116, 101, 114, 75,
  101, 121, 49, 1, 0, 0, 0, 9, 0, 0, 0, 105, 110, 110, 101, 114, 75, 101, 121,
  49, 2, 0, 0, 0, 2, 0, 0, 0, 4, 0, 0, 0, 118, 97, 108, 49, 4, 0, 0, 0, 118, 97,
  108, 50, 2, 0, 0, 0, 4, 0, 0, 0, 118, 97, 108, 51, 4, 0, 0, 0, 118, 97, 108,
  52, 0,
]);

const schema = new BinarySchema(schemaData);

describe("binary schema", () => {
  it("can parse", () => {
    schema.parse();
    const ast = schema.ast;
    expect(ast).toBeDefined();
    expect(ast.bebopVersion).toBe(2);
    expect(ast.definedTypes).keys(["Coordinate", "Point", "Power", "Status"]);
    expect(ast.definedTypes["Status"]).toBeDefined();
    expect(ast.definedTypes["Status"].kind).toBe(4);
    expect(ast.definedTypes["Status"]["members"]).keys(["Okay", "Doable"]);
  });

  it("can read a record using the schema", () => {
    const record = schema.reader.read("Power", recordData);
    expect(record).toBeDefined();
    expect(record["discriminator"]).toBe(2);
    expect(record["value"]).keys(["id", "nested", "point"]);
    const value = record["value"] as Record<string, unknown>;
    expect(value["id"]).toBeInstanceOf(Guid);
    expect((value["id"] as Guid).toString()).toBe(
      "9ff381cf-2347-4aa7-a238-7826027d3f3f"
    );
  });

  it("can write a record using the schema", () => {
    const record = schema.reader.read("Power", recordData);
    expect(record).toBeDefined();
    const data = schema.writer.write("Power", record);
    expect(data).toBeDefined();
    expect(data).toEqual(recordData);
    const read = schema.reader.read("Power", data);
    expect(read).toEqual(record);
  });

  it("cannot modify schema data", () => {
    expect(() => {
      schema.raw[0] = 0;
    }).toThrow(BebopRuntimeError);
  });
});
