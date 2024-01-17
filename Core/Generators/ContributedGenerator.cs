using System;
using System.IO;
using System.Linq;
using Chord.Common;
using Chord.Runtime;
using Core.Exceptions;
using Core.Meta;
using Core.Meta.Extensions;

namespace Core.Generators;

public class ContributedGenerator : BaseGenerator
{
    private readonly Extension _extension;

    public ContributedGenerator(Extension extension)
    {
        _extension = extension;
        if (extension.Contributions is not ChordGenerator chordGenerator)
        {
            throw new InvalidOperationException($"Extension {extension.Name} does not contribute a ChordGenerator.");
        }
        Alias = chordGenerator.Alias;
        Name = chordGenerator.Name;
    }

    public override string Compile(BebopSchema schema, GeneratorConfig config)
    {
        var context = new GeneratorContext(schema, config);
        return _extension.ChordCompile(context.ToString());
    }

    public override AuxiliaryFile? GetAuxiliaryFile()
    {
        if (_extension.PackedFiles is not { Count: > 0 })
        {
            return null;
        }
        var packedFile = _extension.PackedFiles.Where(f => f.Alias == Alias).FirstOrDefault();
        if (packedFile is null)
        {
            return null;
        }
        return new AuxiliaryFile(packedFile.Name, packedFile.Data);
    }

    public override void WriteAuxiliaryFile(string outputPath)
    {
        var auxiliary = GetAuxiliaryFile();
        if (auxiliary is not null)
        {
            if (outputPath.IsPathAttemptingTraversal())
            {
                throw new CompilerException($"Output path {outputPath} is attempting to traverse the directory structure.");
            }
            if (auxiliary.Name.IsPathAttemptingTraversal())
            {
                throw new CompilerException($"Auxiliary file name {auxiliary.Name} is attempting to traverse the directory structure.");
            }
            var filePath = Path.GetFullPath(Path.Join(outputPath, auxiliary.Name));
            File.WriteAllBytes(filePath, auxiliary.Content);
        }
    }

    public override string Alias { get; set; }
    public override string Name { get; set; }
}