namespace Core.Parser
{
    public interface IImportResolver
    {
        string GetPath(string currentFile, string relativeFile);
    }
}
