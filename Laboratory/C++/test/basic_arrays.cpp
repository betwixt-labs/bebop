#include "../gen/basic_arrays.hpp"
#include <cstdlib>

int main(int argc, char **argv) {
    TestInt32Array a = { std::vector<int32_t>(argc, 12345) };
    std::vector<uint8_t> vec {};
    a.encodeInto(vec);
    assert(vec.size() == a.byteCount());
    return 0;
}
