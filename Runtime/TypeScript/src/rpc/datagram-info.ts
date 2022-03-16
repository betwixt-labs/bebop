import { IDatagram } from "./index";
import { RpcRequestDatagram, RpcResponseOk } from "../generated/datagram";

function callId(d: IDatagram): number {
  return d.value?.header?.id ?? 0;
}

function timeoutS(d: IDatagram): number | undefined {
  return d.discriminator == RpcRequestDatagram.discriminator
    ? d.value.header.timeout
    : undefined;
}

function timeoutMs(d: IDatagram): number | undefined {
  let t = timeoutS(d);
  if (t) t *= 1000;
  return t;
}

function isOk(d: IDatagram): boolean {
  return (
    d.discriminator == RpcRequestDatagram.discriminator ||
    d.discriminator == RpcResponseOk.discriminator
  );
}

function isErr(d: IDatagram): boolean {
  return !isOk(d);
}

function isRequest(d: IDatagram): boolean {
  return d.discriminator == RpcRequestDatagram.discriminator;
}

function isResponse(d: IDatagram): boolean {
  return !isRequest(d);
}

function data(d: IDatagram): Uint8Array | undefined {
  switch (d.discriminator) {
    case RpcRequestDatagram.discriminator:
    case RpcResponseOk.discriminator:
      return d.value.data;
    default:
      return undefined;
  }
}

const DatagramInfo = Object.freeze({
  callId,
  timeoutS,
  timeoutMs,
  isOk,
  isErr,
  isRequest,
  isResponse,
  data,
} as const);
export default DatagramInfo;
