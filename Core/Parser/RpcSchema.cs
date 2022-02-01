using System.IO;
using System.Reflection;

namespace Core.Parser
{
    static class RpcSchema
    {
        public static readonly string RpcRequestHeader;

        public static readonly string RpcResponseHeader;

        public static readonly string RpcDatagram;

        static RpcSchema()
        {
            var asm = Assembly.GetExecutingAssembly();
            using (var rsrcStream =
                   asm.GetManifestResourceStream("Core.Parser.RpcRequestHeader.bop"))
            {
                using (var sRdr = new StreamReader(rsrcStream))
                {
                    RpcRequestHeader = sRdr.ReadToEnd();
                }
            }

            using (var rsrcStream =
                   asm.GetManifestResourceStream("Core.Parser.RpcResponseHeader.bop"))
            {
                using (var sRdr = new StreamReader(rsrcStream))
                {
                    RpcResponseHeader = sRdr.ReadToEnd();
                }
            }

            using (var rsrcStream =
                   asm.GetManifestResourceStream("Core.Parser.RpcDatagram.bop"))
            {
                using (var sRdr = new StreamReader(rsrcStream))
                {
                    RpcDatagram = sRdr.ReadToEnd();
                }
            }
        }
    }
}