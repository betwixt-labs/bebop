import {
  CallDetails,
  newPendingResponse,
  PendingResponse,
  ResponseHandle,
} from "./calls";
import * as assert from "assert";
import { BebopRecordImpl, RecordTypeOf } from "../../record";
import {
  IDatagram,
  RemoteRpcErrorVariants,
  TransportError,
  TransportErrorVariants,
} from "../index";
import DatagramInfo from "../datagram-info";
import { UnknownResponseHandler } from "./context";
import {
  RpcDecodeError,
  RpcRequestDatagram,
  RpcResponseCallNotSupported,
  RpcResponseErr,
  RpcResponseInvalidSignature,
  RpcResponseOk,
  RpcResponseUnknownCall,
} from "../../generated/datagram";
import { isUint8Array } from "util/types";
import { RemoteRpcError } from "../error";

const U16_MAX = Math.pow(2, 16) - 1;

/** Verify a given callId is valid. */
export function isValidCallId(id: number): boolean {
  return id > 0 && id <= U16_MAX && Number.isInteger(id);
}

export class RouterCallTable {
  /** Default timeout in seconds */
  private readonly defaultTimeout?: number;
  /** Table of calls which have yet to be resolved. */
  private readonly callTable: Map<
    number,
    [handle: ResponseHandle, timeoutId?: NodeJS.Timeout]
  >;
  /** The next ID value which should be used */
  private nextId: number;

  constructor() {
    if ("BEBOP_RPC_DEFAULT_TIMEOUT" in process.env) {
      const parsedTimeout = parseInt(
        // timeout in seconds
        process.env["BEBOP_RPC_DEFAULT_TIMEOUT"]!,
        10
      );
      assert(
        isFinite(parsedTimeout) && parsedTimeout > 0,
        "Invalid default timeout"
      );
      this.defaultTimeout = parsedTimeout;
    }

    this.nextId = 1;
    this.callTable = new Map();
  }

  /** Get the ID which should be used for the next call that gets made. */
  nextCallId(): number {
    // prevent an infinite loop; if this is a problem in production, we can either increase the
    // id size OR we can stall on overflow and try again creating back pressure.
    assert(this.callTable.size < U16_MAX, "Call table overflow");

    // zero is a "null" id
    while (
      this.nextId == 0 ||
      this.nextId > U16_MAX ||
      this.callTable.has(this.nextId)
    ) {
      // the value is not valid because it is 0 or is in use already
      this.nextId++;
      if (this.nextId > U16_MAX) this.nextId = 1;
    }

    // Very intentional use of POST increment
    return this.nextId++;
  }

  /** Register a datagram before we send it. This will set the call_id. */
  register<RI extends BebopRecordImpl>(
    recordImpl: RI,
    datagram: IDatagram,
    timeoutId?: NodeJS.Timeout
  ): PendingResponse<RecordTypeOf<RI>> {
    assert(
      DatagramInfo.isRequest(datagram),
      "Only requests should be registered."
    );
    const callId = DatagramInfo.callId(datagram);
    assert(isValidCallId(callId), "Datagram must have a valid call id.");
    const timeout = DatagramInfo.timeoutS(datagram) ?? this.defaultTimeout;
    const [handle, pending] = newPendingResponse<RI>(
      recordImpl,
      callId,
      timeout
    );
    this.callTable.set(callId, [handle, timeoutId]);
    return pending;
  }

  /** Receive a datagram and routes it. This is used by the handler for the TransportProtocol. */
  resolve(urh: UnknownResponseHandler | undefined, datagram: IDatagram) {
    assert(
      DatagramInfo.isResponse(datagram),
      "Only responses should be resolved."
    );
    let v: { id: number; res: Uint8Array | RemoteRpcError } | undefined;
    switch (datagram.discriminator) {
      case RpcResponseOk.discriminator:
        v = { id: datagram.value.header.id, res: datagram.value.data };
        break;
      case RpcResponseErr.discriminator:
        v = {
          id: datagram.value.header.id,
          res: new RemoteRpcError({
            discriminator: RemoteRpcErrorVariants.Custom,
            code: datagram.value.code,
            info: datagram.value.info,
          }),
        };
        break;
      case RpcResponseCallNotSupported.discriminator:
        v = {
          id: datagram.value.header.id,
          res: new RemoteRpcError({
            discriminator: RemoteRpcErrorVariants.CallNotSupported,
          }),
        };
        break;
      case RpcResponseUnknownCall.discriminator:
        v = {
          id: datagram.value.header.id,
          res: new RemoteRpcError({
            discriminator: RemoteRpcErrorVariants.UnknownCall,
          }),
        };
        break;
      case RpcResponseInvalidSignature.discriminator:
        v = {
          id: datagram.value.header.id,
          res: new RemoteRpcError({
            discriminator: RemoteRpcErrorVariants.InvalidSignature,
            signature: datagram.value.signature,
          }),
        };
        break;
      case RpcDecodeError.discriminator:
        v = {
          id: datagram.value.header.id,
          res: new RemoteRpcError({
            discriminator: RemoteRpcErrorVariants.RemoteDecode,
            info: datagram.value.info,
          }),
        };
        break;
      case RpcRequestDatagram.discriminator:
        console.error("Requests should not be received at this function");
        break;
      default:
      // do nothing
    }

    if (v && isValidCallId(v.id)) {
      const entry = this.callTable.get(v.id);
      if (!entry) {
        // already handled (probably?)
        return;
      }
      const [call, timeoutId] = entry;
      this.callTable.delete(v.id);
      if (timeoutId) clearTimeout(timeoutId);
      if (isUint8Array(v.res)) {
        call.resolve(v.res);
      } else {
        call.reject(v.res);
      }
    } else if (urh) {
      urh(datagram);
    } else {
      // we don't know what it is and there's no handler for that case
    }
  }

  dropExpired(id: number) {
    const record = this.callTable.get(id);
    if (!record || !CallDetails.isExpired(record[0])) return;

    const [handle, timeoutId] = record;
    this.callTable.delete(id);
    if (timeoutId) clearTimeout(timeoutId!);
    handle.reject(
      new RemoteRpcError({
        discriminator: RemoteRpcErrorVariants.Transport,
        error: new TransportError({
          discriminator: TransportErrorVariants.Timeout,
        }),
      })
    );
  }
}
