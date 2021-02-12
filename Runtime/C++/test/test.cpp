#include <iostream>
#include "../src/bebop.hpp"
#include <cmath>
#include <chrono>
#include <cstddef>
#include <cstdint>
#include <cstdio>

int main() {
    bebop::BebopWriter w;
    const std::string myGuid = "04328465-4290-4bf2-896b-5d05a9084e9b";
    w.writeGuid(bebop::Guid::fromString(myGuid));
    w.writeInt64(12345);
    w.writeFloat32(12.345);
    w.writeByte(255);
    w.writeDate(bebop::TickDuration(123456789));
    size_t p = w.reserveMessageLength();
    w.fillMessageLength(p, 0x1234);

    auto buffer = *w.buffer();
    
    bebop::BebopReader r { buffer.data() };
    std::cout << "guid roundtrip: " << (r.readGuid().toString() == myGuid ? "ok" : "fail") << std::endl;
    std::cout << "int roundtrip: " << (r.readInt64() == 12345 ? "ok" : "fail") << std::endl;
    std::cout << "float roundtrip: " << (fabs(r.readFloat32() - 12.345) < 0.000001 ? "ok" : "fail") << std::endl;
    std::cout << "byte roundtrip: " << (r.readByte() == 255 ? "ok" : "fail") << std::endl;
    std::cout << "date roundtrip: " << (r.readDate().count() == 123456789 ? "ok" : "fail") << std::endl;

    std::cout << "packet dump:";
    for (const auto x : buffer) {
        printf(" %02x", x);
    }
    std::cout << std::endl;
    return 0;
}
