using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Core.Parser.Extensions;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace Compiler.LangServer
{
    public sealed class CompletionHandler : ICompletionHandler
    {
        private readonly BufferManager _bufferManager;
        private readonly DocumentSelector _documentSelector;
        private readonly HashSet<string> _keywords;
        private readonly HashSet<string> _attributes;
        private readonly HashSet<string> _constants;

        public CompletionHandler(BufferManager bufferManager)
        {
            _bufferManager = bufferManager;
            _documentSelector = new DocumentSelector(
                new DocumentFilter()
                {
                    Pattern = "**/*.bop"
                }
            );

            _keywords = new HashSet<string>()
            {
                "enum", "struct", "message",
                "readonly", "map", "array",
                "union", "service",
            };

            _constants = new HashSet<string>()
            {
                "true", "false", "inf", "nan",
            };

            _attributes = new HashSet<string>()
            {
                "opcode", "deprecated", "flags",
            };
        }

        public CompletionRegistrationOptions GetRegistrationOptions(CompletionCapability capability, ClientCapabilities clientCapabilities)
        {
            return new CompletionRegistrationOptions
            {
                DocumentSelector = _documentSelector,
                ResolveProvider = false
            };
        }

        public Task<CompletionList> Handle(CompletionParams request, CancellationToken cancellationToken)
        {
            var items = new List<CompletionItem>();

            var buffer = _bufferManager.GetBuffer(request.TextDocument.Uri);
            if (buffer?.Schema != null)
            {
                items.Add(new CompletionItem
                {
                    Label = buffer.Schema.Value.Namespace,
                    Kind = CompletionItemKind.Reference,
                });

                // TODO: Only top level definitions here?
                foreach (var definition in buffer.Schema.Value.Definitions)
                {
                    items.Add(new CompletionItem
                    {
                        Label = definition.Key,
                        Kind = CompletionItemKind.Variable,
                    });
                }
            }

            // Add attributes
            foreach (var item in _attributes)
            {
                items.Add(new CompletionItem
                {
                    Label = item,
                    Kind = CompletionItemKind.EnumMember,
                });
            }

            // Add types
            foreach (var item in TypeExtensions.BaseTypeNames)
            {
                items.Add(new CompletionItem
                {
                    Label = item.Key,
                    Kind = CompletionItemKind.EnumMember,
                });
            }

            // Add constants
            foreach (var item in _constants)
            {
                items.Add(new CompletionItem
                {
                    Label = item,
                    Kind = CompletionItemKind.Constant,
                });
            }

            // Add keywords
            items.AddRange(
                _keywords.Select(keyword =>
                    new CompletionItem()
                    {
                        Label = keyword,
                        Kind = CompletionItemKind.Keyword,
                    }));

            return Task.FromResult(new CompletionList(items));
        }
    }
}
