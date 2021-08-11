import {makelib} from "./makelib"
import {Library} from "./schema"

process.stdout.write(Library.encode(makelib()))
