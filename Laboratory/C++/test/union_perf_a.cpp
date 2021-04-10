#include "../gen/union_perf_a.hpp"
#include <chrono>
#include <cstdio>
#include <cstdlib>
#include <iostream>
#include <map>
#include <vector>

int main()
{
    using std::chrono::duration;
    using std::chrono::duration_cast;
    using std::chrono::high_resolution_clock;
    using std::chrono::milliseconds;

    bebop::Guid myGuid = bebop::Guid::fromString("81c6987b-48b7-495f-ad01-ec20cc5f5be1");
    const int count = 1000000;
    {
        std::srand(std::time(nullptr));
        auto t1 = high_resolution_clock::now();
        int32_t sum = 0;
        for (int i = 0; i < count; i++)
        {
            A14 inner;
            inner.i14 = std::rand() % 2;
            inner.u = 11111;
            inner.b = true;
            inner.f = 3.14;
            inner.g = myGuid;
            inner.s = "yeah";
            UnionPerfA a;
            a.containerOpcode = 123;
            a.protocolVersion = 456;
            a.u.variant.emplace<A14>(inner);
            std::vector<uint8_t> buf;
            UnionPerfA::encodeInto(a, buf);
            UnionPerfA a2;
            UnionPerfA::decodeInto(buf, a2);
            sum += std::get<A14>(a2.u.variant).i14;
        }
        auto t2 = high_resolution_clock::now();
        auto ms_int = duration_cast<milliseconds>(t2 - t1);
        std::cout << "Computed sum=" << sum << " in " << ms_int.count() << "ms\n";
    }
}
