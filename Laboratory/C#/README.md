# C# Bebop Laboratory

To run the C# tests, from PowerShell:
    dotnet run --project ../../Compiler/ -i ../Schemas/Valid/*.bop build -g "cs:./GeneratedTestCode/Output.g.cs,namespace=Bebop.Codegen"
    dotnet test -nowarn:CS0618
