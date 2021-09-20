#include <cstdio>
#include <vector>
#include "makelib.hpp"

int main()
{
  std::vector<uint8_t> buffer;
  Library::encodeInto(make_library(), buffer);
  std::fwrite(buffer.data(), sizeof(uint8_t), buffer.size(), stdout);
  return 0;
}
