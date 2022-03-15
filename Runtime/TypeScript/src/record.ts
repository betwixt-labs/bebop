import { BebopView } from "./view";

/**
 * The associated functions which define the operations possible on a given Bebop Record type.
 *
 * The record type itself is the `TInterface` which is in the form of `IMyRecord` where `MyRecord`
 * is the record name.
 */
export interface BebopRecordImpl<TInterface = never> {
  encode(message: TInterface): Uint8Array;
  encodeInto(message: TInterface, view: BebopView): number;
  decode(buffer: Uint8Array): TInterface;
  readFrom(view: BebopView): TInterface;
}

export interface BebopUnionRecordImpl<TInterface = never> extends BebopRecordImpl<TInterface> {
  discriminator: number
}
