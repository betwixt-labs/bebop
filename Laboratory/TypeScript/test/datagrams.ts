import {_HelloServiceNameReturn, I_HelloServiceNameReturn} from "./generated/rpc";
import {
    IRpcDatagram,
    IRpcResponseHeader,
    IRpcResponseOk,
    RpcDatagram,
    RpcResponseOk
} from "./generated/datagram";
import {BebopView} from "../../../Runtime/TypeScript";

it("correctly encodes data", () => {
    const name1: I_HelloServiceNameReturn = {value: "KVStore"};
    const name_raw = _HelloServiceNameReturn.encode(name1);
    const name2 = _HelloServiceNameReturn.decode(name_raw);
    expect(name2).toEqual(name1)
})

it("correctly encodes and decodes datagrams", () => {
    const data = new Uint8Array([7, 0, 0, 0, 75, 86, 83, 116, 111, 114, 101]);
    const dgram1: IRpcDatagram = {
        discriminator: RpcResponseOk.discriminator,
        value: {
            header: {id: 1},
            data,
        }
    };
    // {"discriminator":2,"value":{"header":{"id":1},"data":{"0":7,"1":0,"2":0,"3":0,"4":75,"5":86,"6":83,"7":116,"8":111,"9":114,"10":101}}}
    // 17,0,0,0,2,1,0,11,0,0,0,7,0,0,0,75,86,83,116,111,114,101
    const raw = RpcDatagram.encode(dgram1);
    const dgram2 = RpcDatagram.decode(raw);
    expect(dgram2.discriminator).toBe(RpcResponseOk.discriminator);
    if (dgram2.discriminator == RpcResponseOk.discriminator)
        expect(dgram2.value.data).toEqual(data)

    expect(dgram2).toEqual(dgram1);
    expect(dgram2).not.toBe(dgram1);
})

it("correctly encodes and decodes datagrams and data", () => {
    const name1: I_HelloServiceNameReturn = {value: "KVStore"};
    const data = new Uint8Array(_HelloServiceNameReturn.encode(name1));
    expect(data).toEqual(new Uint8Array([7, 0, 0, 0, 75, 86, 83, 116, 111, 114, 101]));

    const dgram1: IRpcDatagram = {
        discriminator: RpcResponseOk.discriminator,
        value: {
            header: { id: 1 },
            data
        }
    };
    // {"discriminator":2,"value":{"header":{"id":1},"data":{"0":7,"1":0,"2":0,"3":0,"4":75,"5":86,"6":83,"7":116,"8":111,"9":114,"10":101}}}
    // 17,0,0,0,2,1,0,11,0,0,0,7,0,0,0,2,1,0,11,0,0,0
    const raw = RpcDatagram.encode(dgram1);
    const dgram2 = RpcDatagram.decode(raw);
    expect(dgram2.discriminator).toBe(RpcResponseOk.discriminator);
    if (dgram2.discriminator == RpcResponseOk.discriminator) {
        expect(dgram2.value.data).toEqual(data);
        const name3 = _HelloServiceNameReturn.decode(dgram2.value.data);
        expect(name3).toEqual(name1);
    }

    expect(dgram2).toEqual(dgram1);
    expect(dgram2).not.toBe(dgram1);
})
