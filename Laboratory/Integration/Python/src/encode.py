from lib import make_lib
from schema import Library
import sys

with open("py.enc", "wb+") as enc_file:
    enc_file.write(bytearray(Library.encode(make_lib())))