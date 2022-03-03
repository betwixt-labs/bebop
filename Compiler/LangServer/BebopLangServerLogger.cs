using System;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;
using OmniSharp.Extensions.LanguageServer.Protocol.Window;

namespace Compiler.LangServer
{
    public sealed class BebopLangServerLogger
    {
        private readonly ILanguageServerFacade _facade;

        public BebopLangServerLogger(ILanguageServerFacade facade)
        {
            _facade = facade ?? throw new ArgumentNullException(nameof(facade));
        }

        public void LogInfo(string message)
        {
            _facade.Window.LogInfo(message);
        }

        public void LogError(string message)
        {
            _facade.Window.LogError(message);
        }

        public void LogError(Exception ex, string? message = null)
        {
            message ??= "An error occured";

            _facade.Window.LogError($"-----------------------------");
            _facade.Window.LogError($"{message}: {ex.Message}");
            _facade.Window.LogError($"{ex.StackTrace}");
            _facade.Window.LogError($"-----------------------------");
        }
    }
}
