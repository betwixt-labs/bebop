import { IDatagram } from "./index";

function callId(d: IDatagram): number {
  return d.value?.header?.id ?? 0;
}

function timeoutS(d: IDatagram): number | undefined {
  return d.discriminator == 1 ? d.value.header.timeout : undefined;
}

function timeoutMs(d: IDatagram): number | undefined {
  let t = timeoutS(d);
  if (t) t *= 1000;
  return t;
}

function isOk(d: IDatagram): boolean {
  return d.discriminator == 1 || d.discriminator == 2;
}

function isErr(d: IDatagram): boolean {
  return !isOk(d);
}

function isRequest(d: IDatagram): boolean {
  return d.discriminator == 1;
}

function isResponse(d: IDatagram): boolean {
  return !isRequest(d);
}

function data(d: IDatagram): Uint8Array | undefined {
  switch (d.discriminator) {
    case 1:
    case 2:
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
