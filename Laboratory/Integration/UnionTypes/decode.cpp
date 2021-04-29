#include "union1.hpp"
#include <cstdio>
#include <vector>
#include <iostream>
#include <fstream>
#include <variant>

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
  Union1 u;
  Union1::decodeInto(bytes, u);
  auto right = std::get_if<Right>(&u.variant);
  if (right != nullptr && right->r == "Success")
    return 0;
  else
    return 1;
}
