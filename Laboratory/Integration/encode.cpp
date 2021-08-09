#include "union1.hpp"
#include <cstdio>
#include <vector>

int main()
{
  std::vector<uint8_t> buffer;
  Union1 u;
  u.variant = Right{"Success"};
  Union1::encodeInto(u, buffer);
  std::fwrite(buffer.data(), sizeof(uint8_t), buffer.size(), stdout);
  return 0;
}
