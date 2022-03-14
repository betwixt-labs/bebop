import { BasicTypes } from './generated/gen';

it("Basic types roundtrip", () => {
    const obj = {
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
        a_string: 'hello world',
        a_guid: '01234567-0123-0123-0123-0123456789ab',
        a_date: new Date(1996, 1, 7),
    };
    const bytes = BasicTypes.encode(obj);
    const obj2 = BasicTypes.decode(bytes);
    expect(obj).toEqual(obj2);
});
