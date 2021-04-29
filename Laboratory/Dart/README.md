# Dart Laboratory

Test with:

    mkdir gen
    dotnet run --project ../../Compiler --files ../Schemas/{array_of_strings,jazz,request}.bop --dart gen/gen.dart
    pub run test
