using System;
using System.IO;
using Core.Parser;
using OmniSharp.Extensions.LanguageServer.Protocol;

namespace Compiler.LangServer
{
    public sealed class BebopLangServerImportResolver : IImportResolver
    {
        private readonly DocumentUri _uri;
        private readonly BebopLangServerLogger _logger;

        public BebopLangServerImportResolver(
            DocumentUri uri,
            BebopLangServerLogger logger)
        {
            _uri = uri ?? throw new ArgumentNullException(nameof(uri));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public string GetPath(string currentFile, string relativeFile)
        {
            var path = DocumentUri.GetFileSystemPath(_uri);
            var directory = Path.GetDirectoryName(path) ?? string.Empty;
            var combined = Path.Combine(directory, relativeFile);
            var absolutePath = Path.GetFullPath(combined);

            _logger.LogInfo($"Resolved {relativeFile} to {absolutePath}");

            return absolutePath;
        }
    }
}
