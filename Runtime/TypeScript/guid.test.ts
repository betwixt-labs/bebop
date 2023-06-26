import { BebopJson, BebopRuntimeError, Guid, GuidMap } from "./index";

import { describe, expect, it } from "vitest";

describe("Guid class", () => {
 it("should create an empty GUID", () => {
    const guid = Guid.empty;
    expect(guid.toString()).toBe("00000000-0000-0000-0000-000000000000");
  });

  it("should identify a Guid instance", () => {
    const guid = Guid.newGuid();
    expect(Guid.isGuid(guid)).toBe(true);
    expect(Guid.isGuid({})).toBe(false);
  });

  it("should check if GUID is empty", () => {
    const guid = Guid.empty;
    expect(guid.isEmpty()).toBe(true);

    const nonEmptyGuid = Guid.newGuid();
    expect(nonEmptyGuid.isEmpty()).toBe(false);
  });

  it("should parse a valid GUID string", () => {
    const parsedGuid = Guid.parseGuid("550e8400-e29b-41d4-a716-446655440000");
    expect(parsedGuid.toString()).toBe("550e8400-e29b-41d4-a716-446655440000");
  });

  it("should throw error when parsing invalid GUID string", () => {
    expect(() => Guid.parseGuid("invalid_guid")).toThrow(BebopRuntimeError);
  });

  it("should create a new Guid instance", () => {
    const guid = Guid.newGuid();
    expect(Guid.isGuid(guid)).toBe(true);
  });

  it("should compare equality of two Guid instances", () => {
    const guid1 = Guid.newGuid();
    const guid2 = Guid.newGuid();

    expect(guid1.equals(guid2)).toBe(false);
    expect(guid1.equals(guid1)).toBe(true);
  });

  it("should write a Guid to a DataView", () => {
    const guid = Guid.newGuid();
    const view = new DataView(new ArrayBuffer(16));

    guid.writeToView(view, 0);

    const readGuid = Guid.fromBytes(new Uint8Array(view.buffer), 0);
    expect(readGuid.equals(guid)).toBe(true);
  });

  it("should read a Guid from a byte array", () => {
    const guid = Guid.newGuid();
    const view = new DataView(new ArrayBuffer(16));

    guid.writeToView(view, 0);
    const readGuid = Guid.fromBytes(new Uint8Array(view.buffer), 0);

    expect(readGuid.equals(guid)).toBe(true);
  });

  it("it should parse uuid" ,() => {
    const guid = Guid.parseGuid("f70ff985-a4ef-4643-bbbc-4a0ed4fc8415");
    const view = new DataView(new ArrayBuffer(16));
    guid.writeToView(view, 0);
    const readGuid = Guid.fromBytes(new Uint8Array(view.buffer), 0);
    expect(guid.toString()).toBe(readGuid.toString());
  });
});

describe("GuidMap", () => {
  it("should set and get values correctly", () => {
    const map = new GuidMap<string>();

    const key1 = Guid.newGuid();
    const key2 = Guid.newGuid();
    const value1 = "Value 1";
    const value2 = "Value 2";

    map.set(key1, value1);
    map.set(key2, value2);

    const retrievedValue1 = map.get(key1);
    const retrievedValue2 = map.get(key2);

    expect(retrievedValue1).toBe(value1);
    expect(retrievedValue2).toBe(value2);
  });

  it("should return undefined for non-existent keys", () => {
    const map = new GuidMap<string>();

    const key = Guid.newGuid();
    const nonExistentKey = Guid.newGuid();
    const value = "Value";

    map.set(key, value);

    const retrievedValue = map.get(nonExistentKey);

    expect(retrievedValue).toBeUndefined();
  });

  it("should delete values correctly", () => {
    const map = new GuidMap<string>();

    const key = Guid.newGuid();
    const value = "Value";

    map.set(key, value);

    const hasKeyBeforeDelete = map.has(key);
    const deleted = map.delete(key);
    const hasKeyAfterDelete = map.has(key);

    expect(hasKeyBeforeDelete).toBe(true);
    expect(deleted).toBe(true);
    expect(hasKeyAfterDelete).toBe(false);
  });

  it("should clear all values correctly", () => {
    const map = new GuidMap<string>();

    const key1 = Guid.newGuid();
    const key2 = Guid.newGuid();
    const value1 = "Value 1";
    const value2 = "Value 2";

    map.set(key1, value1);
    map.set(key2, value2);

    map.clear();

    const hasKey1 = map.has(key1);
    const hasKey2 = map.has(key2);
    const size = map.size;

    expect(hasKey1).toBe(false);
    expect(hasKey2).toBe(false);
    expect(size).toBe(0);
  });

  it("should iterate over entries correctly", () => {
    const map = new GuidMap<string>();

    const key1 = Guid.newGuid();
    const key2 = Guid.newGuid();
    const value1 = "Value 1";
    const value2 = "Value 2";

    map.set(key1, value1);
    map.set(key2, value2);

    const entries: [Guid, string][] = [];

    for (const [key, value] of map.entries()) {
      entries.push([key, value]);
    }

    expect(entries).toEqual([
      [key1, value1],
      [key2, value2],
    ]);
  });

  it("should iterate over keys correctly", () => {
    const map = new GuidMap<string>();

    const key1 = Guid.newGuid();
    const key2 = Guid.newGuid();
    const value1 = "Value 1";
    const value2 = "Value 2";

    map.set(key1, value1);
    map.set(key2, value2);

    const keys: Guid[] = [];

    for (const key of map.keys()) {
      keys.push(key);
    }

    expect(keys).toEqual([key1, key2]);
  });

  it("should iterate over values correctly", () => {
    const map = new GuidMap<string>();

    const key1 = Guid.newGuid();
    const key2 = Guid.newGuid();
    const value1 = "Value 1";
    const value2 = "Value 2";

    map.set(key1, value1);
    map.set(key2, value2);

    const values: string[] = [];

    for (const value of map.values()) {
      values.push(value);
    }

    expect(values).toEqual([value1, value2]);
  });

  it("should iterate over key-value pairs correctly using Symbol.iterator", () => {
    const map = new GuidMap<string>();

    const key1 = Guid.newGuid();
    const key2 = Guid.newGuid();
    const value1 = "Value 1";
    const value2 = "Value 2";

    map.set(key1, value1);
    map.set(key2, value2);

    const entries: [Guid, string][] = [];

    for (const [key, value] of map) {
      entries.push([key, value]);
    }

    expect(entries).toEqual([
      [key1, value1],
      [key2, value2],
    ]);
  });

  it("should create an empty map when constructed with no arguments", () => {
    const map = new GuidMap<number>();
    expect(map.size).toBe(0);
  });

  it("should create a map with entries when constructed with an array of key-value pairs", () => {
    const key1 = Guid.newGuid();
    const key2 = Guid.newGuid();
    const value1 = 10;
    const value2 = 20;
    const entries: readonly (readonly [Guid, number])[] = [
      [key1, value1],
      [key2, value2],
    ];

    const map = new GuidMap<number>(entries);

    expect(map.size).toBe(2);
    expect(map.get(key1)).toBe(value1);
    expect(map.get(key2)).toBe(value2);
  });

  it("should create a map with entries when constructed with an iterable of key-value pairs", () => {
    const key1 = Guid.newGuid();
    const key2 = Guid.newGuid();
    const value1 = 10;
    const value2 = 20;
    const iterable: Iterable<readonly [Guid, number]> = {
      *[Symbol.iterator]() {
        yield [key1, value1];
        yield [key2, value2];
      },
    };

    const map = new GuidMap<number>(iterable);

    expect(map.size).toBe(2);
    expect(map.get(key1)).toBe(value1);
    expect(map.get(key2)).toBe(value2);
  });

  it("should create a map with entries when constructed with an existing Map", () => {
    const key1 = Guid.newGuid();
    const key2 = Guid.newGuid();
    const value1 = 10;
    const value2 = 20;
    const sourceMap = new Map<string, number>([
      [key1.toString(), value1],
      [key2.toString(), value2],
    ]);

    const map = new GuidMap<number>();
    for (const [key, value] of sourceMap.entries()) {
      map.set(Guid.parseGuid(key), value);
    }

    expect(map.size).toBe(2);
    expect(map.get(key1)).toBe(value1);
    expect(map.get(key2)).toBe(value2);
  });

  it("should replacer and revive correctly", () => {
       const data: Record<string, unknown> = {
          "name": "Test",
          "id": Guid.newGuid(),
       };

       console.log(JSON.stringify(data, BebopJson.replacer));
  });
});
