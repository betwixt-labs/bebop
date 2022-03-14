import { IU, U, IWeirdOrder, WeirdOrder } from './generated/gen';

it("Union roundtrip", () => {
    const obj: IU = {
        discriminator: 1,
        value: { b: 12345 },
    };
    const bytes = U.encode(obj);
    const obj2 = U.decode(bytes);
    expect(obj).toEqual(obj2);
});

it("Union weird discriminator order roundtrip", () => {
    const obj: IWeirdOrder = {
        discriminator: 2,
        value: { b: 99 },
    };
    const bytes = WeirdOrder.encode(obj);
    const obj2 = WeirdOrder.decode(bytes);
    expect(obj).toEqual(obj2);
});

