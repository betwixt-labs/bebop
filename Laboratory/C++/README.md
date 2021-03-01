Try this:

    dotnet run --project ../../Compiler --cpp "gen/models.hpp" --files "../Schemas/jazz.bop" "../Schemas/union_perf.bop"
    g++ -std=c++17 test/test.cpp && ./a.out
    g++ -std=c++17 test/union_perf.cpp && ./a.out

