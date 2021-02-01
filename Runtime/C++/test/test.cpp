#include <iostream>
#include "../src/bebop.hpp"
#include <chrono>
#include <cstddef>
#include <cstdint>
#include <cstdio>

int main() {
    auto& w = bebop::BebopWriter::instance();
    const std::string myGuid = "04328465-4290-4bf2-896b-5d05a9084e9b";
    w.writeGuid(myGuid);
    w.writeInt64(12345);
    w.writeByte(255);
    w.writeDate(bebop::TickDuration(123456789));

    auto buffer = *w.buffer();
    
    auto& r = bebop::BebopReader::instance(buffer.data());
    std::cout << "guid roundtrip: " << (r.readGuid() == myGuid ? "ok" : "fail") << std::endl;
    std::cout << "int roundtrip: " << (r.readInt64() == 12345 ? "ok" : "fail") << std::endl;
    std::cout << "byte roundtrip: " << (r.readByte() == 255 ? "ok" : "fail") << std::endl;
    std::cout << "date roundtrip: " << (r.readDate().count() == 123456789 ? "ok" : "fail") << std::endl;
    
    size_t p = w.reserveMessageLength();
    w.fillMessageLength(p, 0x1234);

    std::cout << "packet dump:";
    for (const auto x : buffer) {
        printf(" %02x", x);
    }
    std::cout << std::endl;
    return 0;
}
