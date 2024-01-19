package main

import (
	"bebopc/kernel"
)

//go:wasm-module chord_tinygo
//export chord_compile
func compile(context string) string {

	generatorContext := kernel.DeserializeContext(context)
	builder := kernel.NewIndentedStringBuilder(0)

	builder.AppendLine("This was the raw context:").AppendLine(context)
	builder.AppendLine()
	builder.AppendLine("This is some data from the deserialized context:")
	builder.AppendLine("Config:")
	builder.AppendLine("  Namespace:" + generatorContext.Config.Namespace)
	builder.AppendLine("  OutFile:" + generatorContext.Config.OutFile)
	builder.AppendLine("  Alias:" + generatorContext.Config.Alias)
	builder.AppendLine("This shows we can call the kernel:")
	builder.AppendLine(kernel.GetBebopcVersion())
	return builder.String()
}

// main is required for the `wasi` target, even if it isn't used.
func main() {
}
