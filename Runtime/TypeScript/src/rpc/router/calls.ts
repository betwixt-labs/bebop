import { RouterContext } from "./context";
import { Deadline } from "../deadlines";
import {
  assertUnreachable,
  BebopRecordImpl,
  BebopRuntimeError,
} from "../../index";
import {
  LocalRpcError,
  LocalRpcErrorVariants,
  TransportError,
  TransportErrorDeserializationError,
  TransportErrorVariants,
} from "../error";
import { isNativeError, isUint8Array } from "util/types";
import { RecordTypeOf } from "../../record";
import {
  RpcDecodeError,
  RpcResponseCallNotSupported,
  RpcResponseErr,
  RpcResponseInvalidSignature,
  RpcResponseOk,
  RpcResponseUnknownCall,
} from "../../generated/datagram";
import { IDatagram } from "../index";
import DatagramInfo from "../datagram-info";
import * as assert from "assert";

/** Request handle to allow sending your response to the remote. */
export class RequestHandle implements CallDetails {
  private responseSent = false;
  readonly callDetails: CallDetailsInner;
  private readonly ctx: WeakRef<RouterContext>;

  /** @ignore */
  constructor(ctx: WeakRef<RouterContext>, dgram: IDatagram);
  /** @ignore */
  constructor(rh: RequestHandle);
  constructor(...args: unknown[]) {
    const targs = args as
      | [rh: RequestHandle]
      | [ctx: WeakRef<RouterContext>, dgram: IDatagram];
    if (targs[0] instanceof RequestHandle) {
      const [rh] = targs;
      this.responseSent = rh.responseSent;
      this.callDetails = rh.callDetails;
      this.ctx = rh.ctx;
    } else {
      const [ctx, dgram] = [targs[0], targs[1]!];
      this.ctx = ctx;
      this.callDetails = {
        timeout: DatagramInfo.timeoutS(dgram),
        since: Date.now(),
        callId: DatagramInfo.callId(dgram),
      };
    }
  }

  async sendOkResponseRaw(record: Uint8Array): Promise<void> {
    const ctx = this.$ctx;
    this.verifyNotAlreadyResponded();
    await ctx.send({
      discriminator: RpcResponseOk.discriminator,
      value: {
        header: {
          id: CallDetails.callId(this),
        },
        data: record,
      },
    });
  }

  async sendErrorResponse(code: number, msg?: string): Promise<void> {
    assert(
      code >= 0 && code <= 0xffffffff && Number.isInteger(code),
      "Invalid error code provided. Must be a valid uint32."
    );
    this.verifyNotAlreadyResponded();
    await this.$ctx.send({
      discriminator: RpcResponseErr.discriminator,
      value: {
        header: {
          id: CallDetails.callId(this),
        },
        code,
        info: msg ?? "",
      },
    });
  }

  async sendUnknownCallResponse(): Promise<void> {
    this.verifyNotAlreadyResponded();
    await this.$ctx.send({
      discriminator: RpcResponseUnknownCall.discriminator,
      value: {
        header: { id: CallDetails.callId(this) },
      },
    });
  }

  async sendInvalidSigResponse(expected_sig: number): Promise<void> {
    assert(
      expected_sig >= 0 &&
        expected_sig <= 0xffffffff &&
        Number.isInteger(expected_sig),
      "Invalid expected signature provided. Must be a valid uint32."
    );
    this.verifyNotAlreadyResponded();
    await this.$ctx.send({
      discriminator: RpcResponseInvalidSignature.discriminator,
      value: {
        header: { id: CallDetails.callId(this) },
        signature: expected_sig,
      },
    });
  }

  async sendCallNotSupportedResponse(): Promise<void> {
    this.verifyNotAlreadyResponded();
    await this.$ctx.send({
      discriminator: RpcResponseCallNotSupported.discriminator,
      value: {
        header: {
          id: CallDetails.callId(this),
        },
      },
    });
  }

  async sendDecodeErrorResponse(
    info: string | TransportErrorDeserializationError = ""
  ): Promise<void> {
    this.verifyNotAlreadyResponded();
    await this.$ctx.send({
      discriminator: RpcDecodeError.discriminator,
      value: {
        header: {
          id: CallDetails.callId(this),
        },
        info: typeof info == "string" ? info : info.error.message,
      },
    });
  }

  /** Get a strong reference to the router context and throw a not connected error if it fails */
  private get $ctx(): RouterContext {
    const ctx = this.ctx.deref();
    if (!ctx) {
      throw new TransportError({
        discriminator: TransportErrorVariants.NotConnected,
      });
    }
    return ctx;
  }

  private verifyNotAlreadyResponded(): void {
    if (this.responseSent) {
      throw new TransportError({
        discriminator: TransportErrorVariants.ResponseAlreadySent,
      });
    }
    this.responseSent = true;
  }
}

export class TypedRequestHandle<
  RI extends BebopRecordImpl
> extends RequestHandle {
  constructor(private readonly recordImpl: RI, inner: RequestHandle) {
    super(inner);
  }

  async sendResponse(
    response: RecordTypeOf<RI> | Uint8Array | Error
  ): Promise<void> {
    if (response instanceof LocalRpcError) {
      const e = response.inner;
      switch (e.discriminator) {
        case LocalRpcErrorVariants.DeadlineExceeded:
          // do nothing, no response needed as the remote should forget automatically.
          break;
        case LocalRpcErrorVariants.Custom:
          await this.sendErrorResponse(e.code, e.info);
          break;
        case LocalRpcErrorVariants.NotSupported:
          await this.sendCallNotSupportedResponse();
          break;
        default:
          assertUnreachable(e);
      }
    } else if (isNativeError(response)) {
      console.error("Unhandled exception: ", response);
      await this.sendErrorResponse(
        0xffffffff,
        `Unhandled internal exception: ${response.message}`
      );
    } else {
      await this.sendOkResponse(response);
    }
  }

  async sendOkResponse(record: RecordTypeOf<RI> | Uint8Array): Promise<void> {
    // TODO: Don't clone if https://github.com/RainwayApp/bebop/issues/209 removes the pooled view
    const raw = isUint8Array(record)
      ? record
      : new Uint8Array(this.recordImpl.encode(record));
    return this.sendOkResponseRaw(raw);
  }
}

/** @ignore */
export interface ResponseHandle extends CallDetails {
  resolve(data: Uint8Array): void;

  reject(err: BebopRuntimeError): void;
}

/** @ignore */
export interface PendingResponse<TRecord>
  extends CallDetails,
    PromiseLike<TRecord> {}

/**
 * @ignore
 *
 * @param recordImpl The record implementation which we need for deserializing.
 * @param callId Valid, user-assigned call id.
 * @param timeout Optional timeout in seconds.
 */
export function newPendingResponse<RI extends BebopRecordImpl>(
  recordImpl: RI,
  callId: number,
  timeout?: number
): [tx: ResponseHandle, rx: PendingResponse<RecordTypeOf<RI>>] {
  let resolve: ResponseHandle["resolve"];
  let reject: ResponseHandle["reject"];

  const promise = new Promise<Uint8Array>((res, rej) => {
    resolve = res;
    reject = rej;
  }).then((buf) => recordImpl.decode(buf) as RecordTypeOf<RI>);

  const callDetails: CallDetails["callDetails"] = Object.freeze({
    callId,
    since: Date.now(),
    timeout,
  });

  return [
    {
      resolve: resolve!,
      reject: reject!,
      callDetails,
    },
    {
      then: promise.then.bind(promise),
      callDetails,
    },
  ];
}

interface CallDetailsInner {
  /**
   * How long this call is allowed to be pending for (in seconds). If undefined, no timeout is specified.
   *
   * Warning: No timeout will lead to memory leaks if the transport does not notify the router
   * of dropped/missing data.
   */
  readonly timeout?: number;
  /** The instant at which this call was sent/received. (JS Epoch in ms) */
  readonly since: number;
  /** The unique call ID assigned by us, the caller. */
  readonly callId: number;
}

interface CallDetails {
  readonly callDetails: CallDetailsInner;
}

export const CallDetails = {
  callId(cd: CallDetails): number {
    return cd.callDetails.callId;
  },
  /** JS Epoch of when this was created. */
  since(cd: CallDetails): number {
    return cd.callDetails.since;
  },
  durationMs(cd: CallDetails): number {
    return Date.now() - this.since(cd);
  },
  durationS(cd: CallDetails): number {
    return this.durationMs(cd) / 1000;
  },
  timeoutS(cd: CallDetails): number | undefined {
    return cd.callDetails.timeout === undefined
      ? undefined
      : cd.callDetails.timeout;
  },
  timeoutMs(cd: CallDetails): number | undefined {
    return cd.callDetails.timeout === undefined
      ? undefined
      : cd.callDetails.timeout * 1000;
  },
  isExpired(cd: CallDetails): boolean {
    const timeout = this.timeoutMs(cd);
    return timeout !== undefined && this.durationMs(cd) >= timeout;
  },
  /** JS Epoch at which this expires */
  expiresAt(cd: CallDetails): number | undefined {
    const timeout = this.timeoutMs(cd);
    return timeout === undefined ? undefined : this.since(cd);
  },
  deadline(cd: CallDetails): Deadline {
    return new Deadline(this.expiresAt(cd));
  },
} as const;
