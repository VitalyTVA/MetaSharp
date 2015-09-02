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
                        lineNumber: span.StartLinePosition.Line + 1,
                        columnNumber: span.StartLinePosition.Character + 1,
                        endLineNumber: span.EndLinePosition.Line + 1,
                        endColumnNumber: span.EndLinePosition.Character + 1
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
                    environment.WriteText(outputPath);
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
        readonly string Id, File, Message;
        readonly int LineNumber, ColumnNumber, EndLineNumber, EndColumnNumber;
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
        public Func<string, string> ReadText { get; }
        public Func<string, string> WriteText { get; }
        public Func<Stream, Assembly> LoadAssembly { get; }
        public string IntermediateOutputPath { get; } 
        public Environment(Func<string, string> readText, Func<string, string> writeText, Func<Stream, Assembly> loadAssembly, string intermediateOutputPath) {
            ReadText = readText;
            WriteText = writeText;
            LoadAssembly = loadAssembly;
            IntermediateOutputPath = intermediateOutputPath;
        }
    }
}
