using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Core.Parser;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Core.Exceptions;
using System.Linq;

namespace Tests
{
    [TestClass]
    public class CoreTests
    {
        private readonly string _schemaDir = Path.GetFullPath("../../../../Laboratory/Schemas");
        

        private SchemaParser BuildParser(string path)
        {
            return new SchemaParser(new List<string>() { Path.GetFullPath(Path.Combine(_schemaDir, path)) }, "Test");
        }

        private SchemaParser BuildParser(List<string> paths)
        {
            paths = paths.Select(path => Path.GetFullPath(Path.Combine(_schemaDir, path))).ToList();
            return new SchemaParser(paths, "Test");
        }

        [TestMethod]
        [ExpectedException(typeof(ReferenceScopeException))]
        public async Task TestInvalidUnionReference()
        {
            var parser = BuildParser("ShouldFail/invalid_union_reference.bop");
            var schema = await parser.Parse();
            schema.Validate();
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidUnionBranchException))]
        public async Task TestInvalidNestedUnions()
        {
            var parser = BuildParser("ShouldFail/invalid_union_reference.bop");
            var schema = await parser.Parse();
            schema.Validate();
        }
    }
}
