import {
    Datagram,
    Deadline,
    IDatagram,
    LocalRpcError, LocalRpcErrorVariants,
    Router,
    TransportHandler,
    TransportProtocol
} from "bebop";
import * as EventEmitter from "events";
import {
    IKV,
    KVStoreHandlersDef,
    KVStoreRequests,
    NullServiceHandlersDef,
    NullServiceRequests
} from "./generated/rpc";
import * as assert from "assert";

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
    // this will not be valid at first but once valid will remain so perpetually.
    // Should be valid before the user can send anything.
    private channel!: Channel<Uint8Array>;
    private running = true;

    constructor() {
        super();

        // intentionally do not await this
        this.recvLoop();
    }

    setHandler(recv: TransportHandler): void {
        this.handler = recv;
        this.channel = new Channel(16);
    }

    private async recvLoop(): Promise<void> {
        while (this.running && !this.channel)
            await new Promise(resolve => setTimeout(resolve, 10))
        while (this.running) {
            const datagram = Datagram.decode(await this.channel.rx())
            // awaiting here allows for backpressure on requests, could just spawn instead.
            await this.handler!(datagram)
        }
    }

    async send(datagram: IDatagram): Promise<void> {
        return this.channel.tx(Datagram.encode(datagram))
    }

    shutdown() {
        this.running = false;
        process.nextTick(() => this.channel.close());
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
    const transport = new ChannelTransport();
    // for these tests 1s should be plenty
    setTimeout(() => {
        transport.shutdown()
    }, lifetimeMs);
    return {
        server: Router(NullServiceRequests, transport, new MemBackedKVStore(), undefined),
        client: Router(KVStoreRequests, transport, new NullService(), undefined)
    }
}

it('can request and receive', async () => {
    const {client, server} = setup()
    expect(await client.serviceName()).toBe("KVStore");
    expect(await server.serviceName()).toBe("NullService");
    await client.insert("Mykey", "Myvalue", 1);
    expect(await client.count(5)).toBe(1);
    expect(await client.get("Mykey", 10)).toBe("Myvalue");
})
