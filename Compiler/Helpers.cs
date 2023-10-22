namespace Compiler;

public static class Helpers
{
    public static int ProcessId
    {
        get
        {
            if (_processId == null)
            {
                using var thisProcess = System.Diagnostics.Process.GetCurrentProcess();
                _processId = thisProcess.Id;
            }
            return _processId.Value;
        }
    }
    private static int? _processId;
}