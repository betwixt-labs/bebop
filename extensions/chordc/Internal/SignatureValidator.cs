using Chord.Common.Wasm.Types;

namespace Chord.Compiler.Internal;

public static class SignatureValidator
{
    private static readonly Dictionary<string, List<FunctionSignature>> _signatureLookup;

    /// <summary>
    /// DONT TOUCH THIS CODE
    /// </summary>
    static SignatureValidator()
    {
        // Begin generated code
        _signatureLookup = new Dictionary<string, List<FunctionSignature>>()
        {
                {
                    "as",
                    new ()
                    {
                        new ("write_line",
                            [
                                WebAssemblyValueType.Int32
                            ],
                            [

                            ]
                        ),

                        new ("write_error",
                            [
                                WebAssemblyValueType.Int32
                            ],
                            [

                            ]
                        ),

                        new ("get_bebopc_version",
                            [

                            ],
                            [
                                WebAssemblyValueType.Int32
                            ]
                        ),

                        new ("chord_compile",
                            [
                                WebAssemblyValueType.Int32
                            ],
                            [
                                WebAssemblyValueType.Int32
                            ]
                        )
                    }
                },
                {
                    "javy",
                    new ()
                    {
                        new ("chord-compile",
                            [

                            ],
                            [

                            ]
                        )
                    }
                },
                {
                    "tinygo",
                    new ()
                    {
                        new ("write_line",
                            [
                                WebAssemblyValueType.Int32,
                                WebAssemblyValueType.Int32
                            ],
                            [

                            ]
                        ),

                        new ("write_error",
                            [
                                WebAssemblyValueType.Int32,
                                WebAssemblyValueType.Int32
                            ],
                            [

                            ]
                        ),

                        new ("get_bebopc_version",
                            [
                                WebAssemblyValueType.Int32
                            ],
                            [

                            ]
                        ),

                        new ("chord_compile",
                            [
                                WebAssemblyValueType.Int32,
                                WebAssemblyValueType.Int32,
                                WebAssemblyValueType.Int32
                            ],
                            [

                            ]
                        )
                    }
                },
         };
        // End generated code
    }

    public static FunctionSignature? GetFunctionSignature(string compiler, string functionName)
    {
        if (!_signatureLookup.TryGetValue(compiler, out var signatures))
        {
            // Compiler not found
            return null;
        }

        return signatures.FirstOrDefault(sig => sig.Name == functionName);
    }

    public static bool ValidateSignature(FunctionType? functionType, FunctionSignature? expectedSignature)
    {
        if (functionType == null || expectedSignature == null)
        {
            // Function type or expected signature not found
            return false;
        }

        // Compare the parameters and returns of functionType with expectedSignature
        return CompareSignatures(functionType, expectedSignature);
    }

    private static bool CompareSignatures(FunctionType actual, FunctionSignature expected)
    {
        // Compare the number of parameters
        if (actual.Parameters.Length != expected.Parameters.Length)
        {
            return false;
        }

        // Compare each parameter type
        for (int i = 0; i < actual.Parameters.Length; i++)
        {
            if (actual.Parameters[i] != expected.Parameters[i])
            {
                return false;
            }
        }

        // Compare the number of return types
        if (actual.Returns.Length != expected.Returns.Length)
        {
            return false;
        }

        // Compare each return type
        for (int i = 0; i < actual.Returns.Length; i++)
        {
            if (actual.Returns[i] != expected.Returns[i])
            {
                return false;
            }
        }

        // All checks passed, signatures match
        return true;
    }
}