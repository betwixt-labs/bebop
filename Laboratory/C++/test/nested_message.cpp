#include "../gen/nested_message.hpp"
#include <iostream>
#include <vector>

int main()
{
    {
        OuterM o{InnerM{3}, InnerS{true}};
        std::vector<uint8_t> vec;
        OuterM::encodeInto(o, vec);

        OuterM o2;
        OuterM::decodeInto(vec, o2);

        std::cout << (o2.innerM.value().x.value()) << std::endl;
        std::cout << (o2.innerS.value().y) << std::endl;
    }

    {
        OuterS o{InnerM{4}, InnerS{false}};
        std::vector<uint8_t> vec;
        OuterS::encodeInto(o, vec);

        OuterS o2;
        OuterS::decodeInto(vec, o2);

        std::cout << (o2.innerM.x.value()) << std::endl;
        std::cout << (o2.innerS.y) << std::endl;
    }
}
