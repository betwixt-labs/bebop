import 'dart:typed_data';
import 'package:meta/meta.dart';
import 'package:bebop_dart/bebop_dart.dart';

class Schema1 {
  String prop1;
  Schema1({
    @required this.prop1,
  });

  static Uint8List encode(Schema1 message) {
    final writer = BebopWriter();
    Schema1.encodeInto(message, writer);
    return writer.toList();
  }

  static void encodeInto(Schema1 message, BebopWriter view) {
    view.writeString(message.prop1);
  }

  static Schema1 decode(Uint8List buffer) => Schema1.readFrom(BebopReader(buffer));

  static Schema1 readFrom(BebopReader view) {
    String field0;
    field0 = view.readString();
    return Schema1(prop1: field0);
  }
}

class Schema2 {
  Uint8List schemaBytes;
  Schema2({
    @required this.schemaBytes,
  });

  static Uint8List encode(Schema2 message) {
    final writer = BebopWriter();
    Schema2.encodeInto(message, writer);
    return writer.toList();
  }

  static void encodeInto(Schema2 message, BebopWriter view) {
    view.writeBytes(message.schemaBytes);
  }

  static Schema2 decode(Uint8List buffer) => Schema2.readFrom(BebopReader(buffer));

  static Schema2 readFrom(BebopReader view) {
    Uint8List field0;
    field0 = view.readBytes();
    return Schema2(schemaBytes: field0);
  }
}

