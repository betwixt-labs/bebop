# C# Bebop Laboratory

To run the C# tests, from PowerShell:

    dotnet run --project ..\..\Compiler\ --cs ".\GeneratedTestCode\Output.g.cs" --namespace Bebop.Codegen --files (gci ..\Schemas\Valid\*.bop)
    dotnet test -nowarn:CS0618
