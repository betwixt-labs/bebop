import { RouterContext } from "./context";
import * as assert from "assert";
import { Deadline } from "../deadlines";
import { BebopRecordImpl, BebopRuntimeError } from "../../index";

/** Request handle to allow sending your response to the remote. */
export class RequestHandle<TRecordImpl extends BebopRecordImpl>
  implements ICallDetails
{
  readonly callDetails: ICallDetails["callDetails"];
  private readonly ctx: WeakRef<RouterContext>;
  private readonly recordImpl: TRecordImpl;
}

interface ResponseHandle extends ICallDetails {
  resolve(data: Uint8Array): void;
  reject(err: BebopRuntimeError): void;
}

interface PendingResponse<TRecord> extends ICallDetails, PromiseLike<TRecord> {}

/**
 * @ignore
 *
 * @param recordImpl The record implementation which we need for deserializing.
 * @param callId Valid, user-assigned call id.
 * @param timeout Optional timeout in seconds.
 */
export function newPendingResponse<
  TRecord,
  TRecordImpl extends BebopRecordImpl<TRecord>
>(
  recordImpl: TRecordImpl,
  callId: number,
  timeout?: number
): [ResponseHandle, PendingResponse<TRecord>] {
  let resolve: ResponseHandle["resolve"];
  let reject: ResponseHandle["reject"];

  const promise = new Promise<Uint8Array>((res, rej) => {
    resolve = res;
    reject = rej;
  }).then(recordImpl.decode.bind(recordImpl));

  const callDetails: ICallDetails['callDetails'] = Object.freeze({
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

interface ICallDetails {
  readonly callDetails: {
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
  };
}

export const CallDetails = {
  callId(cd: ICallDetails): number {
    return cd.callDetails.callId;
  },
  /** JS Epoch of when this was created. */
  since(cd: ICallDetails): number {
    return cd.callDetails.since;
  },
  durationMs(cd: ICallDetails): number {
    return Date.now() - this.since(cd);
  },
  durationS(cd: ICallDetails): number {
    return this.durationMs(cd) / 1000;
  },
  timeoutS(cd: ICallDetails): number | undefined {
    return cd.callDetails.timeout === undefined
      ? undefined
      : cd.callDetails.timeout;
  },
  timeoutMs(cd: ICallDetails): number | undefined {
    return cd.callDetails.timeout === undefined
      ? undefined
      : cd.callDetails.timeout * 1000;
  },
  isExpired(cd: ICallDetails): boolean {
    const timeout = this.timeoutMs(cd);
    return timeout !== undefined && this.durationMs(cd) >= timeout;
  },
  /** JS Epoch at which this expires */
  expiresAt(cd: ICallDetails): number | undefined {
    const timeout = this.timeoutMs(cd);
    return timeout === undefined ? undefined : this.since(cd);
  },
  deadline(cd: ICallDetails): Deadline {
    return new Deadline(this.expiresAt(cd));
  },
} as const;
