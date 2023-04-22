import '../gen/gen.dart' as g;
import 'package:test/test.dart';
import 'dart:io';

void main() {
  group('Constant tests', () {
    test('Can access generated constants', () {
      expect(g.exampleConstInt32, equals(-123));
      expect(g.exampleConstUint64, equals(0x123ffffffff));
      expect(g.exampleConstFloat64, equals(123.45678e9));
      expect(g.exampleConstInf, equals(double.infinity));
      expect(g.exampleConstNegInf, equals(double.negativeInfinity));
      expect(g.exampleConstNan, isNaN);
      expect(g.exampleConstFalse, equals(false));
      expect(g.exampleConstTrue, equals(true));
      expect(g.exampleConstString, equals("hello \"world\"\nwith newlines"));
      expect(g.exampleConstGuid, equals("e215a946-b26f-4567-a276-13136f0a1708"));
    });
  });
}
