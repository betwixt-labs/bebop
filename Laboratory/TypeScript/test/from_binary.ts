import * as fs from 'fs';
import { Library } from './generated/gen';
import { Guid } from 'bebop';
if (typeof require !== 'undefined') {
    if (typeof TextDecoder === 'undefined') (global as any).TextDecoder = require('util').TextDecoder;
}
it('can parse Library from binary file', () => {
    var buffer = fs.readFileSync('test/jazz-library.bin');
    var library = Library.decode(buffer);
    expect(library.songs.size).toEqual(1);
    var donnaLee = Guid.parseGuid('81c6987b-48b7-495f-ad01-ec20cc5f5be1');
    expect(library.songs.get(donnaLee).title).toEqual('Donna Lee');
    expect(library.songs.get(donnaLee).performers[1].name).toEqual('Miles Davis');
});
