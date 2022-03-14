import { BasicArrays, IBasicArrays, TestInt32Array } from './generated/gen';

it("Basic array types roundtrip", () => {
    const obj: IBasicArrays = {
        a_bool: [true, false],
        a_byte: Uint8Array.from([1, 11, 111]),
        a_int16: [2, 22, 222],
        a_uint16: [3, 33, 333],
        a_int32: [4, 44, 444],
        a_uint32: [5, 55, 555],
        a_int64: [6n, 66n, 666n],
        a_uint64: [7n, 77n, 777n],
        a_float32: [8, 88, 888],
        a_float64: [9, 99, 999],
        a_string: ['hello world', 'goodbye world'],
        a_guid: ['01234567-0123-0123-0123-0123456789ab', 'ffffffff-eeee-dddd-cccc-bbbbbbbbbbbb'],
    };
    const bytes = BasicArrays.encode(obj);
    const obj2 = BasicArrays.decode(bytes);
    expect(obj).toEqual(obj2);
});

it("Long int array roundtrip", () => {
    const obj = { a: new Array(100_000).fill(12345) };
    const bytes = TestInt32Array.encode(obj);
    const obj2 = TestInt32Array.decode(bytes);
    expect(obj2.a.length).toEqual(100_000);
    expect(obj).toEqual(obj2);
})
