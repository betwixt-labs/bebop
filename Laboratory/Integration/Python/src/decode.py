from schema import Library
from lib import make_lib
import sys
import json

with open(sys.argv[1], "rb") as buffer_file:
    buffer = buffer_file.read()

    de = Library.decode(buffer)
    ex = make_lib()

    eq = json.loads(repr(de)) == json.loads(repr(ex))

    if not eq:
        print("decoded:")
        print(repr(de))
        print()
        print("expected:")
        print(repr(ex))

    sys.exit(0 if eq else 1)