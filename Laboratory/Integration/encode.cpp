#include <cstdio>
#include <vector>
#if defined(_WIN32) || defined(WIN32) || defined(WIN64)
#include <fcntl.h>
#include <io.h>
#endif
#include "makelib.hpp"

int main()
{
#if defined(_WIN32) || defined(WIN32) || defined(WIN64)
  _setmode(_fileno(stdout), _O_BINARY);
#endif
  std::vector<uint8_t> buffer;
  Library::encodeInto(make_library(), buffer);

  std::fwrite(buffer.data(), sizeof(uint8_t), buffer.size(), stdout);
  return 0;
}
