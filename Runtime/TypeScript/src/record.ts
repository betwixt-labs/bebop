import { BebopView } from "./view";

/** Extract the inner type from a BebopRecordImpl type definition */
export type RecordTypeOf<TRecordImpl> = TRecordImpl extends BebopRecordImpl<
  infer TRecord
>
  ? TRecord
  : never;

/** Extract the inner type from a BebopUnionRecordImpl type definition */
export type UnionRecordTypeOf<TRecordImpl> =
  TRecordImpl extends BebopUnionRecordImpl<infer TRecord> ? TRecord : never;

/**
 * The associated functions which define the operations possible on a given Bebop Record type.
 *
 * The record type itself is the `TInterface` which is in the form of `IMyRecord` where `MyRecord`
 * is the record name.
 */
export interface BebopRecordImpl<TInterface = unknown> {
  encode(message: TInterface): Uint8Array;

  encodeInto(message: TInterface, view: BebopView): number;

  decode(buffer: Uint8Array): TInterface;

  readFrom(view: BebopView): TInterface;
}

export interface BebopUnionRecordImpl<TInterface = unknown>
  extends BebopRecordImpl<TInterface> {
  discriminator: number;
}
