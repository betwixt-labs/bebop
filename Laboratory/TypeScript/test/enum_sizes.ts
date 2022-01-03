import * as G from './generated/gen';

it("Supports enum sizes", () => {
    expect(G.SmallEnum.B).toEqual(255);
    expect(typeof(G.SmallEnum.B)).toEqual("number");
    expect(G.HugeEnum.MaxInt.toString()).toEqual(0x7FFFFFFFFFFFFFFFn.toString());
    expect(typeof(G.HugeEnum.MaxInt)).toEqual("bigint");
});
