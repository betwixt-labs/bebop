import { BebopRuntimeError } from "../index";

/** Information about where an error came from. */
export interface ServiceContext {
  /** The service name as it appears in TS. */
  readonly service: string;
  /** The function name as it appears in TS. */
  readonly function: string;
  /** Client-assigned id of this call. */
  readonly callId: number;
}

const noop = () => {};
let ON_RESPOND_ERROR: OnRespondError | undefined;

export type OnRespondError = (
  ctx: ServiceContext,
  err: BebopRuntimeError
) => void;

/**
 * Get the `on_respond_error` function.
 * This should be used by generated code only.
 */
export function get_on_respond_error(): OnRespondError {
  return ON_RESPOND_ERROR ?? noop;
}

/**
 * One-time initialization. Returns true if the provided value has been set. False if it was
 * already initialized.
 */
export function set_on_respond_error(cb: OnRespondError): boolean {
  if (!ON_RESPOND_ERROR) {
    ON_RESPOND_ERROR = cb;
    return true;
  } else {
    return false;
  }
}
