import { ISomeMaps, SomeMaps } from './generated/gen';

it("Map types roundtrip", () => {
    const obj: ISomeMaps = {
        m1: new Map([[false, true], [true, false]]),
        m2: new Map([['a', new Map([['a0k', 'a0v']])], ['b', new Map([['b0k', 'b0v']])]]),
        m3: [new Map([[0, []], [1, [new Map([[false, {x: 1, y: 2}]])]]])],
        m4: [
            new Map([['a', [1, 2, 3]], ['b', [4, 5, 6]]]),
            new Map([['A', [11, 22, 33]], ['B', [44, 55, 66]]]),
        ],
        m5: new Map([['01234567-0123-0123-0123-0123456789ab', { a: 1.5, b: 2.5 }]]),
    };
    const bytes = SomeMaps.encode(obj);
    const obj2 = SomeMaps.decode(bytes);
    expect(obj).toEqual(obj2);
});

