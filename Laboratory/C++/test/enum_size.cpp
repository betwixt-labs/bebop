#include "../gen/enum_size.hpp"
#include <cstdio>

int main() {
  if (sizeof(SmallEnum) != sizeof(uint8_t)) {
    printf("Test failed: incorrect size for HugeEnum\n");
    return 1;
  }
  if (sizeof(HugeEnum) != sizeof(int64_t)) {
    printf("Test failed: incorrect size for HugeEnum\n");
    return 1;
  }
  printf("All tests passed.\n");
  return 0;
}
