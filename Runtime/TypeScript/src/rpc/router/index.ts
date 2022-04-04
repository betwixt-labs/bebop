/**
 * The local end of the pipe handles messages. Implementations are automatically generated from
 * bebop service definitions.
 *
 * You should not implement this by hand but rather the *Handlers traits which are generated.
 */
import { TransportProtocol } from "../transport";
import { RouterContext, UnknownResponseHandler } from "./context";
import { IDatagram } from "../index";
import { RequestHandle } from "./calls";

export { RequestHandle, TypedRequestHandle, CallDetails } from "./calls";
export {
  RouterContext,
  UnknownResponseHandler,
  TransportHandler,
} from "./context";

export interface ServiceHandlers {
  /**
   * The name of this service.
   */
  readonly $name: string;

  /**
   * Use opcode to determine which function to call, whether the signature matches,
   * how to read the buffer, and then convert the returned values and send them as a
   * response
   *
   * This should only be called by the `Router` and is not for external use.
   *
   * @ignore
   */
  $recvCall(datagram: IDatagram, handle: RequestHandle): Promise<void>;
}

/**
 * Wrappers around the process of calling remote functions. Implementations are generated from
 * bebop service definitions.
 *
 * You should not implement this by hand.
 */
export interface ServiceRequests {
  /** The name of this service. */
  readonly $name: string;

  /**
   * The router context. (In TS this object owns it because there is no actual Router.)
   * @ignore
   */
  readonly $ctx: RouterContext;
}

export interface ServiceRequestsConstructor<R extends ServiceRequests> {
  new (ctx: RouterContext): R;
}

export type Router<TRequests> = TRequests & {
  // for now TS does not need anything here.
};

/**
 * Create a new router "class". This is a mix-in-like system that allows calling any of the
 * remote service functions while allowing for additional wrapping logic.
 *
 * @param RemoteServiceConstructor The remote service we are extending into a router. This will be
 * one of the generated `*Requests` classes.
 * @param transport The underlying transport this router uses.
 * @param localService The service which handles incoming requests.
 * @param unknownResponseHandler Optional callback to handle error cases where we do not know
 *  what the `callId` is or when it is an invalid `callId`.
 * @return A router class that can be used with the provided remote service for any
 * transport + local service combination.
 */
export function makeRouter<
  TRequests extends ServiceRequests,
  TRequestsConstructor extends ServiceRequestsConstructor<TRequests>
>(
  RemoteServiceConstructor: TRequestsConstructor,
  transport: TransportProtocol,
  localService: ServiceHandlers,
  unknownResponseHandler?: UnknownResponseHandler
): Router<TRequests> {
  return new RemoteServiceConstructor(
    new RouterContext(transport, localService, unknownResponseHandler)
  );
}
