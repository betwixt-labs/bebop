import { BebopRuntimeError, Guid, GuidMap } from "bebop";
import * as G from "./generated/gen";

it("Roundtrips a flat object to JSON-Over-Bebop", () => {
  const obj: G.IBasicTypes = {
    a_bool: true,
    a_byte: 1,
    a_int16: 2,
    a_uint16: 3,
    a_int32: 4,
    a_uint32: 5,
    a_int64: BigInt(6),
    a_uint64: BigInt(7),
    a_float32: 8,
    a_float64: 9,
    a_string: "hello world",
    a_guid: Guid.parseGuid("01234567-0123-0123-0123-0123456789ab"),
    a_date: new Date(1996, 1, 7),
  };

  const json = G.BasicTypes.encodeToJson(obj);
  const rawParse = JSON.parse(json);
  expect(rawParse).toMatchObject({
    a_bool: true,
    a_byte: 1,
    a_int16: 2,
    a_uint16: 3,
    a_int32: 4,
    a_uint32: 5,
    a_int64: { "#btype": 4, value: "6" },
    a_uint64: { "#btype": 4, value: "7" },
    a_float32: 8,
    a_float64: 9,
    a_string: "hello world",
    a_guid: { "#btype": 5, value: "01234567-0123-0123-0123-0123456789ab" },
    a_date: { "#btype": 2, value: "629592660000000000" },
  });
  expect(() => G.BasicTypes.fromJson(json)).not.toThrow();
});

it("Fails to roundtrip a flat object to JSON-Over-Bebop", () => {
  const obj: G.IBasicTypes = {
    a_bool: true,
    a_byte: 1,
    a_int16: 2,
    a_uint16: 3,
    a_int32: 4,
    a_uint32: 5,
    a_int64: BigInt(6),
    a_uint64: BigInt(7),
    a_float32: 8,
    a_float64: 9,
    a_string: "hello world",
    a_guid: Guid.parseGuid("01234567-0123-0123-0123-0123456789ab"),
    a_date: new Date(1996, 1, 7),
  };
  const json = JSON.stringify(
    obj,
    (key, value) => (typeof value === "bigint" ? value.toString() : value) // return everything else unchanged
  );
  expect(() => G.BasicTypes.fromJson(json)).toThrow(BebopRuntimeError);
});

it("Roundtrips a nested map object to JSON-Over-Bebop", () => {
  const obj: G.ISomeMaps = {
    m1: new Map([
      [false, true],
      [true, false],
    ]),
    m2: new Map([
      ["a", new Map([["a0k", "a0v"]])],
      ["b", new Map([["b0k", "b0v"]])],
    ]),
    m3: [
      new Map([
        [0, []],
        [1, [new Map([[false, { x: 1, y: 2 }]])]],
      ]),
    ],
    m4: [
      new Map([
        ["a", [1, 2, 3]],
        ["b", [4, 5, 6]],
      ]),
      new Map([
        ["A", [11, 22, 33]],
        ["B", [44, 55, 66]],
      ]),
    ],
    m5: new GuidMap([
      [
        Guid.parseGuid("01234567-0123-0123-0123-0123456789ab"),
        { a: 1.5, b: 2.5 },
      ],
    ]),
  };
  const json = G.SomeMaps.encodeToJson(obj);
  expect(() => G.SomeMaps.fromJson(json)).not.toThrow();
});

it("Roundtrips jazz to JSON-Over-Bebop", () => {
  const lib: G.ILibrary = {
    songs: new GuidMap([
      [
        Guid.parseGuid("01234567-0123-0123-0123-0123456789ab"),
        {
          title: "The Song",
          artist: "The Artist",
          performers: [
            {
              name: "The Performer",
              plays: G.Instrument.Sax,
            },
          ],
        },
      ],
    ]),
  };

  const json = G.Library.encodeToJson(lib);
  expect(() => G.Library.fromJson(json)).not.toThrow();
});
