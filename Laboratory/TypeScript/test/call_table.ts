import {RouterCallTable} from "bebop/dist/rpc/router/call-table";
import {newPendingResponse} from "bebop/dist/rpc/router/calls";
import {
    _HelloServiceNameArgs,
    _HelloServiceNameReturn,
    _KVStoreInsertReturn
} from "./generated/rpc";
import {
    CallDetails,
    IDatagram,
    RemoteRpcError,
    RpcRequestDatagram,
    RpcResponseOk,
    TransportError,
    TransportErrorVariants
} from "bebop";
import DatagramInfo from "bebop/dist/rpc/datagram-info";

it("Gets next id", () => {
    const ct = new RouterCallTable()
    expect(ct.nextCallId()).toBe(1)
    expect(ct.nextCallId()).toBe(2)
    ct['nextId'] = Math.pow(2, 16) - 1
    expect(ct.nextCallId()).toBe(0xffff)
    expect(ct.nextCallId()).toBe(1)
})

it("get next id avoids duplicates", () => {
    const ct = new RouterCallTable()
    const [h1, _p1] = newPendingResponse(_KVStoreInsertReturn, 1)
    ct['callTable'].set(CallDetails.callId(h1), h1)
    const [h2, _p2] = newPendingResponse(_KVStoreInsertReturn, 2)
    ct['callTable'].set(CallDetails.callId(h2), h2)
    ct['nextId'] = 0xffff
    expect(ct.nextCallId()).toBe(0xffff)
    expect(ct.nextCallId()).toBe(3)
})

it("registers requests", () => {
    const ct = new RouterCallTable()
    const timeout = 10;
    const data = new Uint8Array([]);
    const d: IDatagram = {
        discriminator: RpcRequestDatagram.discriminator,
        value: {
            header: {
                id: ct.nextCallId(),
                timeout,
                signature: 0
            },
            opcode: 0,
            data
        }
    }
    const pending = ct.register(_HelloServiceNameReturn, d)
    expect(CallDetails.timeoutS(pending)).toBe(timeout)
    expect(CallDetails.callId(pending)).toBe(DatagramInfo.callId(d))
    expect(ct.nextCallId()).toBe(2)
    expect(ct['callTable'].has(1)).toBeTruthy()
})

it("forwards responses", async () => {
    const ct = new RouterCallTable()
    const data_a = new Uint8Array(_HelloServiceNameArgs.encode({}))
    const id = ct.nextCallId()
    const request: IDatagram = {
        discriminator: RpcRequestDatagram.discriminator,
        value: {
            header: {
                id,
                timeout: 0,
                signature: 0
            },
            opcode: 0,
            data: data_a
        }
    }
    const pending = ct.register(_HelloServiceNameReturn, request)
    const data_b = new Uint8Array(_HelloServiceNameReturn.encode({value: "Service"}));
    const response: IDatagram = {
        discriminator: RpcResponseOk.discriminator,
        value: {
            header: {id},
            data: data_b
        }
    }
    ct.resolve(undefined, response)
    const d = await pending
    expect(d.value).toEqual("Service")
    expect(!ct['callTable'].has(id))
})

it("drops expired entry", async () => {
    const ct = new RouterCallTable()
    const request: IDatagram = {
        discriminator: RpcRequestDatagram.discriminator,
        value: {
            header: {
                timeout: 1,
                id: 1,
                signature: 0
            },
            opcode: 0,
            data: new Uint8Array()
        }
    }
    let wasCalled = false;
    const timeoutId = setTimeout(() => { wasCalled = true; }, 1200)
    const pending = ct.register(_HelloServiceNameReturn, request, timeoutId)
    await new Promise(resolve => setTimeout(resolve, 1100))

    expect(CallDetails.isExpired(pending)).toBeTruthy()
    ct.dropExpired(1)
    expect(ct['callTable'].has(1)).toBeFalsy()
    try {
        await pending
        // unreachable in theory
        expect(false).toBeTruthy()
    } catch (err) {
        expect(RemoteRpcError.is(err)).toBeTruthy()
        const rerr = err as RemoteRpcError
        expect(TransportError.is((rerr as any).inner.error)).toBeTruthy()
        const terr = (rerr as any).inner.error as TransportError
        expect(terr.inner.discriminator).toBe(TransportErrorVariants.Timeout)
    }

    expect(wasCalled).toBeFalsy()
    await new Promise(resolve => setTimeout(resolve, 200))
    expect(wasCalled).toBeFalsy()
})
