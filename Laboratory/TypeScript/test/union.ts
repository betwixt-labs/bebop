import { BebopView } from 'bebop';
import { IU, U } from './generated/gen';
import * as assert from "assert";

it("Union roundtrip", () => {
    const obj: IU = {
        discriminator: 1,
        value: { a: 12345 },
    };
    const bytes = U.encode(obj);
    const obj2 = U.decode(bytes);
    expect(obj).toEqual(obj2);
});

