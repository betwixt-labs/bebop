import '../gen/gen.dart';
import 'package:test/test.dart';

void main() {
  group('Tests', () {
    test('ArrayOfStrings', () {
      var test = ArrayOfStrings()..strings = ['abc', 'def'];
      var buffer = ArrayOfStrings.encode(test);
      var expectedBuffer =
          [2, 0, 0, 0, 3, 0, 0, 0, 97] + [98, 99, 3, 0, 0, 0, 100, 101, 102];
      expect(buffer, equals(expectedBuffer));
      expect(ArrayOfStrings.decode(buffer).strings, equals(test.strings));
    });

    test('Furniture', () {
      var test =
          Furniture(family: FurnitureFamily.Bed, name: 'Blåh', price: 123);
      var buffer = Furniture.encode(test);
      var decoded = Furniture.decode(buffer);
      expect(decoded.family, equals(test.family));
      expect(decoded.name, equals(test.name));
      expect(decoded.price, equals(test.price));
    });

    test('Library', () {
      var test = Library()
        ..songs = {
          '4f40c472-2eca-4375-b9c6-f8aa87684579': Song()
            ..title = 'Donna Lee'
            ..year = 1947
            ..performers = [
              Musician(name: 'Charlie Parker', plays: Instrument.Sax),
              Musician(name: 'Miles Davis', plays: Instrument.Trumpet),
            ],
          '2dd446ef-85b4-40fd-91d1-40dbfb93d8c7': Song()
            ..title = 'A Night in Tunisia'
            ..year = 1946
            ..performers = [
              Musician(name: 'Dizzy Gillespie', plays: Instrument.Trumpet),
            ],
        };
      var donna = '4f40c472-2eca-4375-b9c6-f8aa87684579';
      var buffer = Library.encode(test);
      var decoded = Library.decode(buffer);
      expect(decoded.songs, hasLength(2));
      expect(decoded.songs[donna].performers, hasLength(2));
      expect(decoded.songs[donna].performers[1].name, equals('Miles Daaavis'));
    });
  });
}
