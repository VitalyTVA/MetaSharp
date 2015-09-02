using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace MetaSharp {
    //TODO Conditional
    //TODO other environment constants (OutputPath, etc.)
    //TODO g.cs/g.i.cs/designer.cs and explicit file name modes
    //TODO automatically generate namespace and usings based on usings above and under namespace
    //TODO non-cs files generation
    //TODO generate stub types
    //TODO include other files
    //TODO reference other assemblies

    //TODO ADT, immutable objects, DProps, ViewModels, MonadTransfomers, Templates
    public static class Generator {
        public static GeneratorResult Generate(ImmutableArray<string> files, Environment environment, ImmutableArray<string> references) {
            var trees = files.ToImmutableDictionary(x => SyntaxFactory.ParseSyntaxTree(environment.ReadText(x)), x => x);

            var compilation = CSharpCompilation.Create(
                "meta.dll",
                references: references.Select(x => MetadataReference.CreateFromFile(x)),
                options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary),
                syntaxTrees: trees.Keys
            );

            var errors = compilation.GetDiagnostics()
                .Where(x => x.Severity == DiagnosticSeverity.Error)
                .Select(error => {
                    var span = error.Location.GetLineSpan();
                    return new GeneratorError(
                        id: error.Id,
                        file: files.Single(),
                        message: error.GetMessage(),
                        lineNumber: span.StartLinePosition.Line,
                        columnNumber: span.StartLinePosition.Character,
                        endLineNumber: span.EndLinePosition.Line,
                        endColumnNumber: span.EndLinePosition.Character
                        );
                })
                .ToImmutableArray();
            if(errors.Any())
                return new GeneratorResult(ImmutableArray<string>.Empty, errors);
            Assembly compiledAssembly;
            using(var stream = new MemoryStream()) {
                var compileResult = compilation.Emit(stream);
                compiledAssembly = environment.LoadAssembly(stream);
            }
            var result = (string)compiledAssembly.DefinedTypes.Single()
                .GetDeclaredMethod("Do").Invoke(null, null);


            var outputFiles = files
                .Select(file => {
                    var outputPath = Path.Combine(environment.IntermediateOutputPath, file.Replace(".meta.cs", ".meta.g.i.cs"));
                    environment.WriteText(outputPath, result);
                    return outputPath;
                })
                .ToImmutableArray();
            return new GeneratorResult(outputFiles, ImmutableArray<GeneratorError>.Empty);
        }
    }
    public class GeneratorResult {
        public readonly ImmutableArray<string> Files;
        public readonly ImmutableArray<GeneratorError> Errors;
        public GeneratorResult(ImmutableArray<string> files, ImmutableArray<GeneratorError> errors) {
            Files = files;
            Errors = errors;
        }
    }
    public class GeneratorError {
        public readonly string Id, File, Message;
        public readonly int LineNumber, ColumnNumber, EndLineNumber, EndColumnNumber;
        public GeneratorError(string id, string file, string message, int lineNumber, int columnNumber, int endLineNumber, int endColumnNumber) {
            Id = id;
            File = file;
            Message = message;
            LineNumber = lineNumber;
            ColumnNumber = columnNumber;
            EndLineNumber = endLineNumber;
            EndColumnNumber = endColumnNumber;
        }
    }
    public class Environment {
        public readonly Func<string, string> ReadText;
        public readonly Action<string, string> WriteText;
        public readonly Func<MemoryStream, Assembly> LoadAssembly;
        public readonly string IntermediateOutputPath; 
        public Environment(Func<string, string> readText, Action<string, string> writeText, Func<MemoryStream, Assembly> loadAssembly, string intermediateOutputPath) {
            ReadText = readText;
            WriteText = writeText;
            LoadAssembly = loadAssembly;
            IntermediateOutputPath = intermediateOutputPath;
        }
    }
}
