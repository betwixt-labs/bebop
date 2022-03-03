using System.Collections.Concurrent;
using System.Collections.Generic;
using OmniSharp.Extensions.LanguageServer.Protocol;

namespace Compiler.LangServer
{
    public sealed class BufferManager
    {
        private readonly ConcurrentDictionary<DocumentUri, Buffer> _buffers = new ConcurrentDictionary<DocumentUri, Buffer>();
        private readonly BebopLangServerLogger _logger;

        public BufferManager(BebopLangServerLogger logger)
        {
            _logger = logger ?? throw new System.ArgumentNullException(nameof(logger));
        }

        public bool IsOpened(DocumentUri documentPath)
        {
            return _buffers.ContainsKey(documentPath);
        }

        public void UpdateBuffer(DocumentUri documentPath, Buffer buffer)
        {
            _buffers.AddOrUpdate(documentPath, buffer, (_, _) => buffer);
        }

        public Buffer? GetBuffer(DocumentUri documentPath)
        {
            return _buffers.TryGetValue(documentPath, out var buffer) ? buffer : null;
        }

        internal bool RemoveBuffer(DocumentUri documentPath, out int? version, out List<string>? imports)
        {
            var result = _buffers.TryRemove(documentPath, out var buffer);
            version = buffer?.Version;
            imports = buffer?.Schema?.Imports;

            if (result)
            {
                var versionString = version?.ToString() ?? "null";
                _logger.LogInfo($"Removed buffer for document: {documentPath} ({versionString})");
            }

            return result;
        }
    }
}