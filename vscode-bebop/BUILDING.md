# Building

## Build dependencies

Run the following commands to prepare the VSCode extension:

```powershell
# Build Compiler
dotnet build ../Compiler/ -c Debug

# Install dependencies
npm install

# Run esbuild
npm run esbuild
```

## Run extensions

Open the `bebop/vscode-bebop` folder in `vscode` and press `F5`.