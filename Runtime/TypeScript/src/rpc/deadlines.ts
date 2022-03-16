export class Deadline {
  /** JS Epoch time at which this deadline will occur (in ms) */
  private readonly epoch?: number;

  /**
   * @param epoch The JS Epoch time at which this deadline occurs.
   */
  constructor(epoch?: number) {
    this.epoch = epoch;
  }

  /**
   * True if this deadline is in the past.
   */
  hasPassed(): boolean {
    return Date.now() > (this.epoch ?? Number.MAX_VALUE);
  }

  /**
   * True if this deadline is in the future.
   */
  isBefore(): boolean {
    return !this.hasPassed();
  }

  /**
   * The instant at which this deadline is scheduled for.
   * (In ms since JS epoch).
   */
  get at(): number | undefined {
    return this.epoch;
  }

  /**
   * How long do we have left before the deadline.
   */
  get remainingMs(): number | undefined {
    return this.epoch === undefined ? undefined : Date.now() - this.epoch;
  }

  /**
   * How long do we have left before the deadline.
   */
  get remainingS(): number | undefined {
    return this.epoch === undefined
      ? undefined
      : (Date.now() - this.epoch) / 1000;
  }

  /** The system time at which this deadline is scheduled for. */
  atDate(): Date | undefined {
    return this.epoch === undefined ? undefined : new Date(this.epoch);
  }
}
