dotnet run --project ..\..\Compiler --ts "test\generated\gen.ts" --files (gci ..\Schemas\Valid\*.bop)
dotnet run --project ..\..\Compiler --ts "test\generated\rpc.ts" --files ..\Schemas\rpc.bop
dotnet run --project ..\..\Compiler --ts "test\generated\datagram.ts" --files ..\..\Core\Schemas\RpcDatagram.bop
