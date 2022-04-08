import { ServiceHandlers } from "./index";
import { TransportProtocol } from "../transport";
import { IDatagram } from "../index";
import { RouterCallTable } from "./call-table";
import DatagramInfo from "../datagram-info";
import { BebopRecordImpl, RecordTypeOf } from "../../record";
import { RpcDecodeError, RpcRequestDatagram } from "../../generated/datagram";
import * as assert from "assert";
import { RequestHandle } from "./calls";

export type UnknownResponseHandler = (unknownDatagram: IDatagram) => void;

export class RouterContext {
  /** Callback that receives any datagrams without a call id. */
  private readonly unknownResponseHandler?: UnknownResponseHandler;

  /** Local service handles requests from the remote. */
  private readonly localService: ServiceHandlers;

  private readonly transport: TransportProtocol;

  private readonly callTable: RouterCallTable;

  constructor(
    transport: TransportProtocol,
    localService: ServiceHandlers,
    unknownResponseHandler?: UnknownResponseHandler
  ) {
    this.transport = transport;
    this.localService = localService;
    this.unknownResponseHandler = unknownResponseHandler;
    this.callTable = new RouterCallTable();

    transport.setHandler(RouterContext.makeTransportHandler(new WeakRef(this)));
  }

  /** @ignore */
  async request<RI extends BebopRecordImpl, RO extends BebopRecordImpl>(
    recordInputImpl: RI,
    recordOutputImpl: RO,
    opcode: number,
    timeout: number | undefined,
    signature: number,
    record: RecordTypeOf<RI>
  ): Promise<RecordTypeOf<RO>> {
    return this.requestRaw(
      recordOutputImpl,
      opcode,
      timeout,
      signature,
      // TODO: Don't clone if https://github.com/RainwayApp/bebop/issues/209 removes the pooled view
      new Uint8Array(recordInputImpl.encode(record))
    );
  }

  /**
   * Send a raw byte request to the remote. This is used by the generated code.
   *
   * @ignore
   */
  async requestRaw<RO extends BebopRecordImpl>(
    recordOutputImpl: RO,
    opcode: number,
    timeout: number | undefined,
    signature: number,
    data: Uint8Array
  ): Promise<RecordTypeOf<RO>> {
    assert(
      !timeout ||
        (timeout >= 0 && timeout <= 0xffff && Number.isInteger(timeout)),
      "Timeout must be a valid uint16 or undefined"
    );
    assert(
      opcode >= 0 && opcode <= 0xffff && Number.isInteger(opcode),
      "Opcode must be a valid uint16"
    );
    assert(
      signature >= 0 && signature <= 0xffffffff && Number.isInteger(signature),
      "Signature must be a valid uint32"
    );
    const id = this.callTable.nextCallId();
    const datagram: IDatagram = {
      discriminator: RpcRequestDatagram.discriminator,
      value: {
        header: {
          id,
          timeout: timeout ?? 0,
          signature,
        },
        opcode,
        data,
      },
    };

    const expiresInMs = DatagramInfo.timeoutMs(datagram);
    let timeoutId;
    if (expiresInMs) timeoutId = this.cleanOnExpiration(id, expiresInMs);
    const pending = this.callTable.register(
      recordOutputImpl,
      datagram,
      timeoutId
    );
    await this.send(datagram);
    return pending;
  }

  /**
   * Send a request to the remote. This is used by the generated code.
   *
   * @ignore
   */
  async send(datagram: IDatagram): Promise<void> {
    return this.transport.send(datagram);
  }

  /**
   * Send notification that there was an error decoding one of the datagrams. This may be called
   * by the transport or by the generated handler code.
   */
  sendDecodeErrorResponse(callId = 0, info = ""): Promise<void> {
    return this.send({
      discriminator: RpcDecodeError.discriminator,
      value: {
        header: {
          id: callId,
        },
        info,
      },
    });
  }

  /**
   * Receive a request datagram and send it to the local service for handling.
   * This is used by the handler for the TransportProtocol.
   *
   * @ignore
   */
  private recvRequest(
    datagram: IDatagram,
    handle: RequestHandle
  ): Promise<void> {
    assert(DatagramInfo.isRequest(datagram), "Datagram must be a request");
    return this.localService.$recvCall(datagram, handle);
  }

  /**
   * Receive a response datagram and pass it to the call table to resolve the correct future.
   * This is used by the handler for the TransportProtocol.
   *
   * @ignore
   */
  private recvResponse(datagram: IDatagram) {
    assert(DatagramInfo.isResponse(datagram), "Datagram must be a response");
    this.callTable.resolve(this.unknownResponseHandler, datagram);
  }

  private cleanOnExpiration(id: number, inMs: number): NodeJS.Timeout {
    const weak = new WeakRef(this);
    return setTimeout(() => {
      const self = weak.deref();
      if (!self) return;
      self.callTable.dropExpired(id);
    }, inMs);
  }

  private static makeTransportHandler(
    weakCtx: WeakRef<RouterContext>
  ): TransportHandler {
    return async (d: IDatagram) => {
      const ctx = weakCtx.deref();
      if (!ctx) return;

      if (DatagramInfo.isRequest(d)) {
        await ctx.recvRequest(d, new RequestHandle(weakCtx, d));
      } else {
        ctx.recvResponse(d);
      }
    };
  }
}

/** A connector between the Router and the Handlers. This is set up internally. */
export type TransportHandler = (datagram: IDatagram) => Promise<void>;
