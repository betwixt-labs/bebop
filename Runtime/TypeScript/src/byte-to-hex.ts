function makeByteToHex(): string[] {
  const a: string[] = [];
  const hexDigits = "0123456789abcdef";
  for (const x of hexDigits) {
    for (const y of hexDigits) {
      a.push(x + y);
    }
  }
  return a;
}

// A lookup table: ['00', '01', ..., 'ff']
const byteToHex = Object.freeze(makeByteToHex());

export default byteToHex;
