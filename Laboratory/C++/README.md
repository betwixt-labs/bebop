Try this:

    cd test
    dotnet run --project ../../../Compiler --cpp "../gen/jazz.hpp" --files "../../Schemas/jazz.bop"
    g++ -std=c++17 test.cpp && ./a.out
