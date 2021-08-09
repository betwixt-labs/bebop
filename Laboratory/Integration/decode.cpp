#include "makelib.hpp"
#include <iostream>
#include <fstream>

static std::vector<uint8_t> ReadAllBytes(char const *filename)
{
  std::ifstream ifs(filename, std::ios::binary | std::ios::ate);
  std::ifstream::pos_type pos = ifs.tellg();
  std::vector<uint8_t> result(pos);
  ifs.seekg(0, std::ios::beg);
  ifs.read(reinterpret_cast<char *>(&result[0]), pos);
  return result;
}

int main(int argc, char **argv)
{
  const auto bytes = ReadAllBytes(argv[1]);
  Library lib;
  Library::decodeInto(bytes, lib);

  // because there is no default object comparison, just serialize again to make sure we got all data.
  std::vector<uint8_t> buffer;
  Library::encodeInto(lib, buffer);
  return buffer != bytes;
}
