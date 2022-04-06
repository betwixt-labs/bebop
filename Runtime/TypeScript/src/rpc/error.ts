import { assertUnreachable, BebopRuntimeError } from "../index";

/** Information about where an error came from. */
export interface ServiceContext {
  /** The service name as it appears in TS. */
  readonly service: string;
  /** The function name as it appears in TS. */
  readonly function: string;
  /** Client-assigned id of this call. */
  readonly callId: number;
}

const noop = () => {
  /* Do nothing */
};

let ON_RESPOND_ERROR: OnRespondError | undefined;

export type OnRespondError = (
  ctx: ServiceContext,
  err: BebopRuntimeError
) => void;

/**
 * Get the `on_respond_error` function.
 * This should be used by generated code only.
 */
export function getOnRespondError(): OnRespondError {
  return ON_RESPOND_ERROR ?? noop;
}

/**
 * One-time initialization. Returns true if the provided value has been set. False if it was
 * already initialized.
 */
export function setOnRespondError(cb: OnRespondError): boolean {
  if (!ON_RESPOND_ERROR) {
    ON_RESPOND_ERROR = cb;
    return true;
  } else {
    return false;
  }
}

/**
 * Use this with the `.catch` for a promise which we want to have the error
 * handled globally for.
 *
 * @ignore
 */
export function handleRespondError(
  err: unknown,
  service: string,
  fnName: string,
  callId: number
): void {
  if (ON_RESPOND_ERROR && err instanceof BebopRuntimeError) {
    ON_RESPOND_ERROR({ service, function: fnName, callId }, err);
  } else {
    throw err;
  }
}

export enum TransportErrorVariants {
  ResponseAlreadySent,
  DatagramTooLarge,
  SerializationError,
  DeserializationError,
  NotConnected,
  Timeout,
  CallDropped,
  Other,
}

/**
 * A response can only be sent once.
 */
export type TransportErrorResponseAlreadySent = {
  readonly discriminator: TransportErrorVariants.ResponseAlreadySent;
};

/**
 * The datagram is too large to send. Some transports may impose different limits, but Bebop
 * itself has a 4GiB limit on the user data as it gets serialized into a byte array.
 */
export type TransportErrorDatagramTooLarge = {
  readonly discriminator: TransportErrorVariants.DatagramTooLarge;
};

/** The serialization failed. */
export type TransportErrorSerializationError = {
  readonly discriminator: TransportErrorVariants.SerializationError;
  readonly error: BebopRuntimeError;
};

/**
 * Was unable to parse data received from the remote. This indicates an issue with the
 * transport as signatures would catch non-agreeing structures.
 */
export type TransportErrorDeserializationError = {
  readonly discriminator: TransportErrorVariants.DeserializationError;
  readonly error: BebopRuntimeError;
};

/**
 * The transport is not connected to the remote. Ideally the transport should handle this
 * internally rather than returning this error unless there is no way for it to reconnect.
 */
export type TransportErrorNotConnected = {
  readonly discriminator: TransportErrorVariants.NotConnected;
};

/** Provided timeout was reached before we received a response. */
export type TransportErrorTimeout = {
  readonly discriminator: TransportErrorVariants.Timeout;
};

/** The request handle was dropped and we no longer care about any received responses. */
export type TransportErrorCallDropped = {
  readonly discriminator: TransportErrorVariants.CallDropped;
};

/** A custom transport-specific error. */
export type TransportErrorOther = {
  readonly discriminator: TransportErrorVariants.Other;
  readonly message: string;
};

export type TransportErrorInner =
  | TransportErrorResponseAlreadySent
  | TransportErrorDatagramTooLarge
  | TransportErrorSerializationError
  | TransportErrorDeserializationError
  | TransportErrorNotConnected
  | TransportErrorTimeout
  | TransportErrorCallDropped
  | TransportErrorOther;

/**
 * Things that could go wrong with the underlying transport, need it to be somewhat generic.
 * Things like the internet connection dying would fall under this.
 */
export class TransportError extends BebopRuntimeError {
  public readonly inner: TransportErrorInner;

  static is(obj: any): obj is TransportError {
    return (
      obj?.constructor?.name == TransportError.name &&
      Number.isInteger(obj?.inner?.discriminator)
    );
  }

  constructor(inner: TransportErrorInner) {
    let message: string;
    switch (inner.discriminator) {
      case TransportErrorVariants.ResponseAlreadySent:
        message = "Response already sent";
        break;
      case TransportErrorVariants.DatagramTooLarge:
        message = "Datagram too large";
        break;
      case TransportErrorVariants.SerializationError:
        message = `Serialization error: ${inner.error.message}`;
        break;
      case TransportErrorVariants.DeserializationError:
        message = `Deserialization error: ${inner.error.message}`;
        break;
      case TransportErrorVariants.NotConnected:
        message = "Not connected";
        break;
      case TransportErrorVariants.Timeout:
        message = "Timeout";
        break;
      case TransportErrorVariants.CallDropped:
        message = "Call dropped";
        break;
      case TransportErrorVariants.Other:
        message = inner.message;
        break;
      default:
        assertUnreachable(inner);
    }
    super(message);
    this.inner = inner;
  }
}

export enum LocalRpcErrorVariants {
  Custom,
  DeadlineExceeded,
  NotSupported,
}

/**
 * Custom error code with a message. You should use an Enum to keep the error codes synchronized.
 */
export type LocalRpcErrorCustom = {
  readonly discriminator: LocalRpcErrorVariants.Custom;
  readonly code: number;
  readonly info: string;
};

/**
 * This may be returned if the deadline provided to the function has been exceeded, however, it
 * is also acceptable to return any other error OR okay value instead and a best-effort will be
 * made to return it.
 *
 * Warning: Do not return this if there was no deadline provided to the function.
 */
export type LocalRpcErrorDeadlineExceeded = {
  readonly discriminator: LocalRpcErrorVariants.DeadlineExceeded;
};

/**
 * Indicates a given operation has not been implemented for the given service.
 */
export type LocalRpcErrorNotSupported = {
  readonly discriminator: LocalRpcErrorVariants.NotSupported;
};

export type LocalRpcErrorInner =
  | LocalRpcErrorCustom
  | LocalRpcErrorDeadlineExceeded
  | LocalRpcErrorNotSupported;

/** Errors that the local may return when sending or responding to a request. */
export class LocalRpcError extends BebopRuntimeError {
  public readonly inner: LocalRpcErrorInner;

  static is(obj: any): obj is LocalRpcError {
    return (
      obj?.constructor?.name == LocalRpcError.name &&
      Number.isInteger(obj?.inner?.discriminator)
    );
  }

  constructor(inner: LocalRpcErrorInner) {
    let message: string;
    switch (inner.discriminator) {
      case LocalRpcErrorVariants.DeadlineExceeded:
        message = "Deadline exceeded";
        break;
      case LocalRpcErrorVariants.Custom:
        message = `Custom (${inner.code}) ${inner.info}`;
        break;
      case LocalRpcErrorVariants.NotSupported:
        message = "Not supported";
        break;
      default:
        assertUnreachable(inner);
    }
    super(message);
    this.inner = inner;
  }
}

export enum RemoteRpcErrorVariants {
  Transport,
  Custom,
  UnknownCall,
  InvalidSignature,
  CallNotSupported,
  RemoteDecode,
}

/** A transport error occurred whilst waiting for a response. */
export type RemoteRpcErrorTransport = {
  readonly discriminator: RemoteRpcErrorVariants.Transport;
  readonly error: TransportError;
};

/**
 * The remote generated a custom error message. You should use an Enum to keep the error codes
 * synchronized.
 */
export type RemoteRpcErrorCustom = {
  readonly discriminator: RemoteRpcErrorVariants.Custom;
  readonly code: number;
  readonly info?: string;
};

/**
 * The remote did not recognize the opcode. This means the remote either dropped support for
 * this call or the remote is outdated.
 */
export type RemoteRpcErrorUnknownCall = {
  readonly discriminator: RemoteRpcErrorVariants.UnknownCall;
};

/**
 * Our call signature did not match that of the remote which means there is probably a version
 * mismatch or possibly they are exposing a different service than expected.
 */
export type RemoteRpcErrorInvalidSignature = {
  readonly discriminator: RemoteRpcErrorVariants.InvalidSignature;
  readonly signature: number;
};

/** There is no implementation for this call on the remote at this time. */
export type RemoteRpcErrorCallNotSupported = {
  readonly discriminator: RemoteRpcErrorVariants.CallNotSupported;
};

/** Remote was not able to decode our message. */
export type RemoteRpcErrorRemoteDecode = {
  readonly discriminator: RemoteRpcErrorVariants.RemoteDecode;
  readonly info?: string;
};

export type RemoteRpcErrorInner =
  | RemoteRpcErrorTransport
  | RemoteRpcErrorCustom
  | RemoteRpcErrorUnknownCall
  | RemoteRpcErrorInvalidSignature
  | RemoteRpcErrorCallNotSupported
  | RemoteRpcErrorRemoteDecode;

/** Errors that can be received from the remote when making a request. */
export class RemoteRpcError extends BebopRuntimeError {
  public readonly inner: RemoteRpcErrorInner;

  static is(obj: any): obj is RemoteRpcError {
    return (
      obj?.constructor?.name == RemoteRpcError.name &&
      Number.isInteger(obj?.inner?.discriminator)
    );
  }

  constructor(inner: RemoteRpcErrorInner) {
    let message: string;
    switch (inner.discriminator) {
      case RemoteRpcErrorVariants.UnknownCall:
        message = "Unknown call";
        break;
      case RemoteRpcErrorVariants.Transport:
        message = inner.error.message;
        break;
      case RemoteRpcErrorVariants.Custom:
        message = `Custom (${inner.code}) ${inner.info}`;
        break;
      case RemoteRpcErrorVariants.InvalidSignature:
        message = "Invalid signature";
        break;
      case RemoteRpcErrorVariants.CallNotSupported:
        message = "Call not supported";
        break;
      case RemoteRpcErrorVariants.RemoteDecode:
        message = `Remote decode ${inner.info}`;
        break;
      default:
        assertUnreachable(inner);
    }
    super(message);
    this.inner = inner;
  }
}
