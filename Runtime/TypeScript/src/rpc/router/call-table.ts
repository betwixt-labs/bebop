import {
  CallDetails,
  newPendingResponse,
  PendingResponse,
  ResponseHandle,
} from "./calls";
import * as assert from "assert";
import { BebopRecordImpl } from "../../record";
import { IDatagram } from "../index";
import DatagramInfo from "../datagram-info";
import { UnknownResponseHandler } from "./context";
import {
  IRpcDecodeError,
  RpcDecodeError,
  RpcRequestDatagram,
  RpcResponseCallNotSupported,
  RpcResponseErr,
  RpcResponseInvalidSignature,
  RpcResponseOk,
  RpcResponseUnknownCall,
} from "../../generated/datagram";
import { BebopRuntimeError } from "../../index";
import { isUint8Array } from "util/types";
import { AssertionError } from "assert";

const U16_MAX = 1 << 16;

/** Verify a given callId is valid. */
export function isValidCallId(id: number): boolean {
  return id > 0 && id <= U16_MAX && Number.isInteger(id);
}

export class RouterCallTable {
  /** Default timeout in seconds */
  private readonly defaultTimeout?: number;
  /** Table of calls which have yet to be resolved. */
  private readonly callTable: Map<number, ResponseHandle>;
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
    while (this.nextId == 0 || this.callTable.has(this.nextId)) {
      // the value is not valid because it is 0 or is in use already
      this.nextId++;
      if (this.nextId > U16_MAX) {
        this.nextId = 1;
      }
    }

    // Very intentional use of POST increment
    return this.nextId++;
  }

  /** Register a datagram before we send it. This will set the call_id. */
  register<TRecord, TRecordImpl extends BebopRecordImpl<TRecord>>(
    recordImpl: TRecordImpl,
    datagram: IDatagram
  ): PendingResponse<TRecord> {
    assert(
      DatagramInfo.isRequest(datagram),
      "Only requests should be registered."
    );
    const callId = DatagramInfo.callId(datagram);
    assert(isValidCallId(callId), "Datagram must have a valid call id.");
    const timeout = DatagramInfo.timeoutS(datagram) ?? this.defaultTimeout;
    const [handle, pending] = newPendingResponse<TRecord, TRecordImpl>(
      recordImpl,
      callId,
      timeout
    );
    this.callTable.set(callId, handle);
    return pending;
  }

  /** Receive a datagram and routes it. This is used by the handler for the TransportProtocol. */
  resolve(urh: UnknownResponseHandler | undefined, datagram: IDatagram) {
    assert(
      DatagramInfo.isResponse(datagram),
      "Only responses should be resolved."
    );
    let v: { id: number; res: Uint8Array | BebopRuntimeError } | undefined;
    switch (datagram.discriminator) {
      case RpcResponseOk.discriminator:
        v = { id: datagram.value.header.id, res: datagram.value.data };
        break;
      case RpcResponseErr.discriminator:
        v = {
          id: datagram.value.header.id,
          res: new BebopRuntimeError(
            `Custom Error (${datagram.value.code}): ${datagram.value.info}`
          ),
        };
        break;
      case RpcResponseCallNotSupported.discriminator:
        v = {
          id: datagram.value.header.id,
          res: new BebopRuntimeError("Call not supported"),
        };
        break;
      case RpcResponseUnknownCall.discriminator:
        v = {
          id: datagram.value.header.id,
          res: new BebopRuntimeError("Unknown call"),
        };
        break;
      case RpcResponseInvalidSignature.discriminator:
        v = {
          id: datagram.value.header.id,
          res: new BebopRuntimeError("Invalid signature"),
        };
        break;
      case RpcDecodeError.discriminator:
        const msg = datagram.value.info
          ? `Decode error: ${datagram.value.info}`
          : "Decode error";
        v = {
          id: datagram.value.header.id,
          res: new BebopRuntimeError(msg),
        };
        break;
      case RpcRequestDatagram.discriminator:
        console.error("Requests should not be received at this function");
        break;
      default:
      // do nothing
    }

    if (v && isValidCallId(v.id)) {
      const call = this.callTable.get(v.id);
      if (!call) {
        // already handled (probably?)
        return;
      }
      this.callTable.delete(v.id);
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
    if (record && CallDetails.isExpired(record)) {
      this.callTable.delete(id);
    }
  }
}