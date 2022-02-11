using System.IO;
using System.Reflection;

namespace Core.Schemas
{
    /// <summary>
    /// Provides easy access to hard-coded schemas that can be used by generators or parsers to inject additional schema
    /// components using Bebop files.
    /// </summary>
    static class SchemaRepo
    {
        public static readonly string RpcDatagram;

        static SchemaRepo()
        {
            var asm = Assembly.GetExecutingAssembly();

            using (var rsrcStream =
                   asm.GetManifestResourceStream("Core.Schemas.RpcDatagram.bop"))
            {
                using (var sRdr = new StreamReader(rsrcStream))
                {
                    RpcDatagram = sRdr.ReadToEnd();
                }
            }
        }
    }
}
