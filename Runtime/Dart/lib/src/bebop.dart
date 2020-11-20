import 'dart:convert';
import 'dart:math';
import 'dart:typed_data';
import 'tables.dart';

/// A wrapper around a ByteBuffer for reading Bebop base types from it.
///
/// It is used by the code that `bebopc --lang dart` generates. You shouldn't
/// need to use it directly.
class BebopReader {
  ByteBuffer _buffer;
  Uint8List _bytes;
  ByteData _view;
  int _index = 0;

  final Utf8Decoder _utf8Decoder = Utf8Decoder();

  BebopReader._();
  static final BebopReader _instance = BebopReader._();
  factory BebopReader.fromBuffer(ByteBuffer buffer) => _instance..load(buffer);
  factory BebopReader(Uint8List list) => BebopReader.fromBuffer(list.buffer);

  void load(ByteBuffer buffer) {
    _buffer = buffer;
    _bytes = Uint8List.view(buffer);
    _view = ByteData.view(_buffer);
    _index = 0;
  }

  void skip(int amount) {
    _index += amount;
  }

  int readByte() => _bytes[_index++];

  int readUint16() {
    final v = _view.getUint16(_index, Endian.little);
    _index += 2;
    return v;
  }

  int readInt16() {
    final v = _view.getInt16(_index, Endian.little);
    _index += 2;
    return v;
  }

  int readUint32() {
    final v = _view.getUint32(_index, Endian.little);
    _index += 4;
    return v;
  }

  int readInt32() {
    final v = _view.getInt32(_index, Endian.little);
    _index += 4;
    return v;
  }

  int readUint64() {
    final v = _view.getUint64(_index, Endian.little);
    _index += 8;
    return v;
  }

  int readInt64() {
    final v = _view.getInt64(_index, Endian.little);
    _index += 8;
    return v;
  }

  double readFloat32() {
    final v = _view.getFloat32(_index, Endian.little);
    _index += 4;
    return v;
  }

  double readFloat64() {
    final v = _view.getFloat64(_index, Endian.little);
    _index += 8;
    return v;
  }

  bool readBool() => readByte() != 0;

  Uint8List readBytes() {
    final length = readUint32();
    final view = _buffer.asUint8List(_index, length);
    _index += length;
    return view;
  }

  String readString() => _utf8Decoder.convert(readBytes());

  String readGuid() {
    var s = byteToHex[_bytes[_index + 3]], d = '-';
    s += byteToHex[_bytes[_index + 2]];
    s += byteToHex[_bytes[_index + 1]];
    s += byteToHex[_bytes[_index]];
    s += d;
    s += byteToHex[_bytes[_index + 5]];
    s += byteToHex[_bytes[_index + 4]];
    s += d;
    s += byteToHex[_bytes[_index + 7]];
    s += byteToHex[_bytes[_index + 6]];
    s += d;
    s += byteToHex[_bytes[_index + 8]];
    s += byteToHex[_bytes[_index + 9]];
    s += d;
    s += byteToHex[_bytes[_index + 10]];
    s += byteToHex[_bytes[_index + 11]];
    s += byteToHex[_bytes[_index + 12]];
    s += byteToHex[_bytes[_index + 13]];
    s += byteToHex[_bytes[_index + 14]];
    s += byteToHex[_bytes[_index + 15]];
    _index += 16;
    return s;
  }

  DateTime readDate() {
    final low = readUint32();
    final high = readUint32() & 0x3fffffff;
    final msSince1AD = 429496.7296 * high + 0.0001 * low;
    return DateTime.fromMillisecondsSinceEpoch(
        (msSince1AD - 62135596800000).round());
  }

  T readEnum<T>(List<T> values) => values[readUint32()];

  int readMessageLength() => readUint32();
}

/// A wrapper around a ByteBuffer for writing Bebop base types from it.
///
/// It is used by the code that `bebopc --lang dart` generates. You shouldn't
/// need to use it directly.
class BebopWriter {
  ByteBuffer _buffer;
  Uint8List _bytes = Uint8List(256);
  ByteData _view;
  int _length = 0;

  final Utf8Encoder _utf8Encoder = Utf8Encoder();

  BebopWriter._() {
    _buffer = _bytes.buffer;
    _view = ByteData.view(_buffer);
  }

  static final BebopWriter _instance = BebopWriter._();
  factory BebopWriter() => _instance.._length = 0;

  void _guaranteeBufferLength(int length) {
    if (length > _bytes.lengthInBytes) {
      final data = Uint8List(min(2 * _bytes.lengthInBytes, length));
      data.setAll(0, _bytes);
      _bytes = data;
      _buffer = data.buffer;
      _view = ByteData.view(_buffer);
    }
  }

  void _growBy(int amount) {
    _length += amount;
    _guaranteeBufferLength(_length);
  }

  void writeByte(int value) {
    final index = _length;
    _growBy(1);
    _bytes[index] = value;
  }

  void writeUint16(int value) {
    final index = _length;
    _growBy(2);
    _view.setUint16(index, value, Endian.little);
  }

  void writeInt16(int value) {
    final index = _length;
    _growBy(2);
    _view.setInt16(index, value, Endian.little);
  }

  void writeUint32(int value) {
    final index = _length;
    _growBy(4);
    _view.setUint32(index, value, Endian.little);
  }

  void writeInt32(int value) {
    final index = _length;
    _growBy(4);
    _view.setInt32(index, value, Endian.little);
  }

  void writeUint64(int value) {
    final index = _length;
    _growBy(8);
    _view.setUint64(index, value, Endian.little);
  }

  void writeInt64(int value) {
    final index = _length;
    _growBy(8);
    _view.setInt64(index, value, Endian.little);
  }

  void writeFloat32(double value) {
    final index = _length;
    _growBy(4);
    _view.setFloat32(index, value, Endian.little);
  }

  void writeFloat64(double value) {
    final index = _length;
    _growBy(8);
    _view.setFloat64(index, value, Endian.little);
  }

  void writeBool(bool value) => writeByte(value ? 1 : 0);

  void writeBytes(Uint8List value) {
    writeUint32(value.length);
    final index = _length;
    _growBy(value.length);
    _bytes.setAll(index, value);
  }

  void writeString(String value) {
    writeBytes(_utf8Encoder.convert(value));
  }

  void writeGuid(String value) {
    var p = 0, a = 0;
    final i = _length, c = value.codeUnits;
    _growBy(16);
    a = a << 4 | asciiToHex[c[p++]];
    a = a << 4 | asciiToHex[c[p++]];
    a = a << 4 | asciiToHex[c[p++]];
    a = a << 4 | asciiToHex[c[p++]];
    a = a << 4 | asciiToHex[c[p++]];
    a = a << 4 | asciiToHex[c[p++]];
    a = a << 4 | asciiToHex[c[p++]];
    a = a << 4 | asciiToHex[c[p++]];
    if (c[p] == 45) p++;
    _view.setUint32(i, a, Endian.little);
    a = 0;
    a = a << 4 | asciiToHex[c[p++]];
    a = a << 4 | asciiToHex[c[p++]];
    a = a << 4 | asciiToHex[c[p++]];
    a = a << 4 | asciiToHex[c[p++]];
    if (c[p] == 45) p++;
    _view.setUint16(i + 4, a, Endian.little);
    a = 0;
    a = a << 4 | asciiToHex[c[p++]];
    a = a << 4 | asciiToHex[c[p++]];
    a = a << 4 | asciiToHex[c[p++]];
    a = a << 4 | asciiToHex[c[p++]];
    if (c[p] == 45) p++;
    _view.setUint16(i + 6, a, Endian.little);
    a = 0;
    a = a << 4 | asciiToHex[c[p++]];
    a = a << 4 | asciiToHex[c[p++]];
    a = a << 4 | asciiToHex[c[p++]];
    a = a << 4 | asciiToHex[c[p++]];
    if (c[p] == 45) p++;
    a = a << 4 | asciiToHex[c[p++]];
    a = a << 4 | asciiToHex[c[p++]];
    a = a << 4 | asciiToHex[c[p++]];
    a = a << 4 | asciiToHex[c[p++]];
    _view.setUint32(i + 8, a, Endian.big);
    a = 0;
    a = a << 4 | asciiToHex[c[p++]];
    a = a << 4 | asciiToHex[c[p++]];
    a = a << 4 | asciiToHex[c[p++]];
    a = a << 4 | asciiToHex[c[p++]];
    a = a << 4 | asciiToHex[c[p++]];
    a = a << 4 | asciiToHex[c[p++]];
    a = a << 4 | asciiToHex[c[p++]];
    a = a << 4 | asciiToHex[c[p++]];
    _view.setUint32(i + 12, a, Endian.big);
  }

  void writeDate(DateTime date) {
    final ms = date.millisecondsSinceEpoch;
    final msSince1AD = ms + 62135596800000;
    final low = (msSince1AD % 429496.7296 * 10000).round();
    final high = (msSince1AD / 429496.7296).round() | 0x40000000;
    writeUint32(low);
    writeUint32(high);
  }

  void writeEnum(dynamic value) {
    writeUint32(value.index);
  }

  /// Reserve some space to write a message's length prefix, and return its index.
  /// The length is stored as a little-endian fixed-width unsigned 32-bit integer, so 4 bytes are reserved.
  int reserveMessageLength() {
    final i = _length;
    _growBy(4);
    return i;
  }

  /// Fill in a message's length prefix.
  void fillMessageLength(int position, int messageLength) {
    _view.setUint32(position, messageLength, Endian.little);
  }

  Uint8List toList() => Uint8List.view(_buffer, 0, _length);
}
