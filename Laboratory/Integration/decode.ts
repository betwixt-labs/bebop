import {Library, ILibrary, IAlbum} from "./schema";
import {makelib} from "./makelib";
import fs from "fs";
import equal from "deep-equal";
import {assert} from "console";
import util from "util";

const buffer = fs.readFileSync(process.argv[2]);
const de = Library.decode(buffer);
const ex = makelib();

const eq = equal(de, ex, {strict: false});
if (!eq) {
    console.log("decoded:")
    console.log(util.inspect(de, {showHidden: false, depth: null}))
    console.log()
    console.log("expected:")
    console.log(util.inspect(ex, {showHidden: false, depth: null}))
}

process.exit(eq ? 0 : 1);
