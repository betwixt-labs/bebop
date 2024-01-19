using Chord.Runtime.Internal.Strings;
using Spectre.Console;
using Wasmtime;

namespace Chord.Runtime.Internal.Callers;

internal sealed class JavyCaller : WasmCaller
{
    private Extension? _extension;

    internal JavyCaller(Instance wasmInstance, StringMarshaler stringMarshaler, ExtensionRuntime runtime) : base(wasmInstance, stringMarshaler, runtime)
    {

    }

    public override void SetExtension(Extension extension)
    {
        _extension = extension;
    }

    public override async ValueTask<string> ChordCompileAsync(string context, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(context, nameof(context));

        var compile = _instance.GetAction("chord-compile");
        if (compile == null)
        {
            throw new ExtensionRuntimeException("chord-compile function not found");
        }
        if (_extension == null)
        {
            throw new ExtensionRuntimeException("Extension not set");
        }
        await _extension.ClearStandardOutput(cancellationToken);
        await _extension.ClearStandardError(cancellationToken);
        await _extension.ClearStandardInput(cancellationToken);

        await _extension.WriteStandardInput(context, cancellationToken);

        compile();

        var result = await _extension.ReadStandardOutput(cancellationToken);
        var standardError = await _extension.ReadStandardError(cancellationToken);
        if (!string.IsNullOrWhiteSpace(standardError))
        {
            _runtime.StandardError.MarkupLineInterpolated($"{standardError}");
        }
        if (string.IsNullOrWhiteSpace(result))
        {
            throw new ExtensionRuntimeException("chord-compile function did not return a result");
        }
        return result;
    }
}