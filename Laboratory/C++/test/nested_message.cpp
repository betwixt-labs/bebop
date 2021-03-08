#include "../gen/nested_message.hpp"
#include <iostream>
#include <vector>

int main() {
    NestedOuter o { NestedInner { 3 }};
    std::vector<uint8_t> vec;
    NestedOuter::encodeInto(o, vec);
    
    NestedOuter o2;
    NestedOuter::decodeInto(vec.data(), o2);

    std::cout << (o2.inner.value().x.value()) << std::endl;
}

