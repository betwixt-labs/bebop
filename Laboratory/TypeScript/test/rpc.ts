import {
    Datagram,
    Deadline,
    IDatagram,
    LocalRpcError, LocalRpcErrorVariants,
    Router,
    makeRouter,
    TransportHandler,
    TransportProtocol,
} from "bebop";
import * as EventEmitter from "events";
import {
    _HelloServiceNameReturn,
    I_HelloServiceNameReturn,
    IKV,
    KVStoreHandlersDef,
    KVStoreRequests,
    NullServiceHandlersDef,
    NullServiceRequests
} from "./generated/rpc";
import * as assert from "assert";
import {RpcResponseOk} from "../../../Runtime/TypeScript/dist/generated/datagram";

/** Simple MPMC channel which uses an event emitter internally */
class Channel<T> {
    private readonly buffer: T[] = [];
    private readonly maxSize: number;

    private readonly events: EventEmitter = new EventEmitter();
    private closed = false;


    constructor(bufferSize?: number) {
        assert(bufferSize && Number.isInteger(bufferSize) && bufferSize > 0, "Must be a valid buffer size!")
        this.maxSize = bufferSize;
    }

    async tx(v: T): Promise<void> {
        if (this.closed) throw new Error("Channel closed")
        while (this.buffer.length >= this.maxSize) {
            await new Promise((resolve, reject) => {
                this.events
                    .once('rem', resolve)
                    .once('close', () => reject(new Error("Channel closed")))
            })
            this.events.removeAllListeners('close')
        }

        this.buffer.push(v);
        this.events.emit('add');
    }

    async rx(): Promise<T> {
        if (this.closed) throw new Error("Channel closed")
        while (this.buffer.length <= 0) {
            await new Promise((resolve, reject) => {
                this.events
                    .once('add', resolve)
                    .once('close', () => reject(new Error("Channel Closed")))
            })
            this.events.removeAllListeners('close')
        }
        const v = this.buffer.shift()!
        this.events.emit('rem')
        return v
    }

    close(): void {
        this.events.emit('close')
    }
}

class ChannelTransport extends TransportProtocol {
    private handler?: TransportHandler;

    static make(): [a: ChannelTransport, b: ChannelTransport] {
        const c1 = new Channel<Uint8Array>(8);
        const c2 = new Channel<Uint8Array>(8);

        const running: [boolean] = [true];
        return [
            new ChannelTransport(running, c1.tx.bind(c1), c2.rx.bind(c2), c1, c2),
            new ChannelTransport(running, c2.tx.bind(c2), c1.rx.bind(c1), c1, c2)
        ]
    }

    private constructor(
        private readonly running: [ptr: boolean],
        private readonly tx: (v: Uint8Array) => Promise<void>,
        private readonly rx: () => Promise<Uint8Array>,
        private readonly ch1: Channel<Uint8Array>,
        private readonly ch2: Channel<Uint8Array>,
    ) {
        super();

        // intentionally do not await this
        this.recvLoop();
    }

    setHandler(recv: TransportHandler): void {
        this.handler = recv;
    }

    private async recvLoop(): Promise<void> {
        while (this.running[0] && !this.handler)
            await 0
        while (this.running[0]) {
            // awaiting here allows for backpressure on requests, could just spawn instead.
            const raw = await this.rx();
            const datagram = Datagram.decode(raw)
            await this.handler!(datagram)
        }
    }

    async send(datagram: IDatagram): Promise<void> {
        const raw = Datagram.encode(datagram);
        return this.tx(raw);
    }

    shutdown() {
        this.running[0] = false;
        process.nextTick(() => {
            this.ch1.close()
            this.ch2.close()
        });
    }
}

class MemBackedKVStore extends KVStoreHandlersDef {
    private readonly store: Map<string, string>;

    constructor() {
        super();
        this.store = new Map();
    }

    async count(deadline: Deadline): Promise<bigint> {
        return BigInt(this.store.size)
    }

    async entries(deadline: Deadline, page: bigint, pageSize: number): Promise<Array<IKV>> {
        // yeah, I know this is not efficient, but JS really lacks iterator support and this is just
        // an example
        const start = Math.min(this.store.size, Number(page) * pageSize);
        const end = Math.min(start + pageSize, this.store.size);
        return Array.from(this.store.entries()).slice(start, end).map(([key, value]) => ({
            key,
            value
        }))
    }

    async get(deadline: Deadline, key: string): Promise<string> {
        const value = this.store.get(key);
        if (value === undefined) {
            throw new LocalRpcError({
                discriminator: LocalRpcErrorVariants.Custom,
                code: 1,
                info: "Unknown key"
            })
        } else {
            return value
        }
    }

    async insert(deadline: Deadline, key: string, value: string): Promise<boolean> {
        if (this.store.has(key)) return false;
        this.store.set(key, value);
        return true;
    }

    async insertMany(deadline: Deadline, entries: Array<IKV>): Promise<Array<string>> {
        const notAdded = []
        for (const {key, value} of entries) {
            if (this.store.has(key)) {
                notAdded.push(key)
            } else {
                this.store.set(key, value)
            }
        }
        return notAdded
    }

    async keys(deadline: Deadline, page: bigint, pageSize: number): Promise<Array<string>> {
        const start = Math.min(this.store.size, Number(page) * pageSize);
        const end = Math.min(start + pageSize, this.store.size);
        return Array.from(this.store.keys()).slice(start, end);
    }

    async ping(deadline: Deadline): Promise<void> {
    }

    async values(deadline: Deadline, page: bigint, pageSize: number): Promise<Array<string>> {
        const start = Math.min(this.store.size, Number(page) * pageSize);
        const end = Math.min(start + pageSize, this.store.size);
        return Array.from(this.store.values()).slice(start, end);
    }
}

class NullService extends NullServiceHandlersDef {
}

function setup(lifetimeMs = 1000): { server: Router<NullServiceRequests>, client: Router<KVStoreRequests> } {
    const [transport_a, transport_b] = ChannelTransport.make();
    // for these tests 1s should be plenty
    setTimeout(() => {
        transport_a.shutdown()
        transport_b.shutdown()
    }, lifetimeMs);
    return {
        server: makeRouter(NullServiceRequests, transport_a, new MemBackedKVStore(), undefined),
        client: makeRouter(KVStoreRequests, transport_b, new NullService(), undefined)
    }
}

it("correctly encodes data", () => {
    const name1: I_HelloServiceNameReturn = { value: "KVStore" };
    const name_raw = _HelloServiceNameReturn.encode(name1);
    const name2 = _HelloServiceNameReturn.decode(name_raw);
    expect(name2).toEqual(name1)
})

it("correctly encodes and decodes datagrams", () => {
    const data = new Uint8Array([7,0,0,0,75,86,83,116,111,114,101]);
    const dgram1: IDatagram = {
        discriminator: RpcResponseOk.discriminator,
        value: {
            header: {id: 1},
            data,
        }
    };
    const raw = Datagram.encode(dgram1);
    const dgram2 = Datagram.decode(raw);
    expect(dgram2.discriminator).toBe(RpcResponseOk.discriminator);
    if (dgram2.discriminator == RpcResponseOk.discriminator)
        expect(dgram2.value.data).toEqual(data)

    expect(dgram2).toEqual(dgram1);
    expect(dgram2).not.toBe(dgram1);
})

it("correctly encodes and decodes datagrams and data", () => {
    const name1: I_HelloServiceNameReturn = { value: "KVStore" };
    const data = _HelloServiceNameReturn.encode(name1);

    const dgram1: IDatagram = {
        discriminator: RpcResponseOk.discriminator,
        value: {
            header: {id: 1},
            data: data
        }
    };
    const raw = Datagram.encode(dgram1);
    const dgram2 = Datagram.decode(raw);
    expect(dgram2.discriminator).toBe(RpcResponseOk.discriminator);
    if (dgram2.discriminator == RpcResponseOk.discriminator) {
        const name3 = _HelloServiceNameReturn.decode(dgram2.value.data);
        expect(name3).toEqual(name1)
    }

    expect(dgram2).toEqual(dgram1);
    expect(dgram2).not.toBe(dgram1);
})

it("underlying transport works", async () => {
    const [transport_a, transport_b] = ChannelTransport.make();
    setTimeout(() => {
        transport_a.shutdown()
        transport_b.shutdown()
    }, 100);

    let a_recv = null, b_recv = null;
    transport_a.setHandler(async (d: IDatagram) => {
        a_recv = d;
    });
    transport_b.setHandler(async (d: IDatagram) => {
        b_recv = d;
    });

    const d1: IDatagram = {
        discriminator: 1,
        value: {
            header: {id: 1, signature: 0, timeout: 0},
            opcode: 1,
            data: new Uint8Array(0)
        },
    };
    await transport_a.send(d1);
    expect(b_recv).toEqual(d1);
    expect(b_recv).not.toBe(d1);

    b_recv = null;
    await transport_a.send(d1);
    expect(b_recv).toEqual(d1);
    expect(a_recv).toBeNull();

    b_recv = null;
    await transport_b.send(d1);
    expect(a_recv).toEqual(d1);
    expect(a_recv).not.toBe(d1);
    expect(b_recv).toBeNull();
})

it('can request and receive', async () => {
    const {client, server} = setup(100000)
    expect(await client.serviceName()).toBe("KVStore");
    expect(await server.serviceName()).toBe("NullService");
    await client.insert("Mykey", "Myvalue", 1);
    expect(await client.count(5)).toBe(1);
    expect(await client.get("Mykey", 10)).toBe("Myvalue");
})
