import { BebopView } from 'bebop';
import { IU, U } from './generated/union';
import * as assert from "assert";

it("Union roundtrip", () => {
    const obj: IU = {
        discriminator: 'A',
        value: { a: 12345 },
    };
    const bytes = U.encode(obj);
    const obj2 = U.decode(bytes);
    expect(obj).toEqual(obj2);
});

