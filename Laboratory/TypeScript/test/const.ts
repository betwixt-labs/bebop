import { BebopView, Guid } from 'bebop';
import * as G from './generated/gen';
if (typeof require !== 'undefined') {
    if (typeof TextDecoder === 'undefined') (global as any).TextDecoder = require('util').TextDecoder;
}
it("Constants are generated", () => {
    expect(G.exampleConstInt32).toEqual(-123);
    expect(G.exampleConstUint64).toEqual(0x123ffffffffn);
    expect(G.exampleConstFloat64).toEqual(123.45678e9);
    expect(G.exampleConstInf).toEqual(Infinity);
    expect(G.exampleConstNegInf).toEqual(-Infinity);
    expect(G.exampleConstNan).toEqual(NaN);
    expect(G.exampleConstFalse).toEqual(false);
    expect(G.exampleConstTrue).toEqual(true);
    expect(G.exampleConstString).toEqual("hello \"world\"\nwith newlines");
    expect(G.exampleConstGuid.equals(Guid.parseGuid("e215a946-b26f-4567-a276-13136f0a1708"))).toBe(true);
});

