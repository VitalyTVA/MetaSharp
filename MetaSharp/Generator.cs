#define TEST
#define TEST
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace MetaSharp {
    //TODO exceptions in generator methods
    //TODO non static classes
    //TODO several output files
    //TODO Conditional
    //TODO other environment constants (OutputPath, etc.)
    //TODO g.cs/g.i.cs/designer.cs and explicit file name modes
    //TODO automatically generate namespace and usings based on usings above and under namespace
    //TODO non-cs files generation
    //TODO generate stub types
    //TODO include other files
    //TODO reference other assemblies
    //TODO use SourceText with SyntaxFactory
    //TODO use SourceReferenceResolver?
    //TODO option to insert delimeters between output from different classes and methods

    //TODO ADT, immutable objects, DProps, ViewModels, MonadTransfomers, Templates, Localization, Aspects
    //TODO binary output - drawing images??
    public static class Generator {
        const string DefaultSuffix = "meta";
        const string CShaprFileExtension = ".cs";
        const string DefaultInputFileEnd = DefaultSuffix + CShaprFileExtension;
        const string DefaultOutputFileEnd_IntellisenseVisible = DefaultSuffix + ".g.i" + CShaprFileExtension;
        const string DefaultAssemblyName = "meta.dll";
        const string NewLine = "\r\n";
        const string ConditionalConstant = "METASHARP";

        public static bool IsMetaSharpFile(string fileName) {
            return fileName.EndsWith(DefaultInputFileEnd);
        }

        public static GeneratorResult Generate(ImmutableArray<string> files, Environment environment, ImmutableArray<string> references) {
            var parseOptions = CSharpParseOptions.Default.WithPreprocessorSymbols(ConditionalConstant);
            var trees = files.ToImmutableDictionary(x => SyntaxFactory.ParseSyntaxTree(environment.ReadText(x), parseOptions), x => x);


            var compilation = CSharpCompilation.Create(
                DefaultAssemblyName,
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
                        file: trees[error.Location.SourceTree],
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

            var typeTreeMap = compilation
                .GetSymbolsWithName(name => true, SymbolFilter.Type)
                .Cast<INamedTypeSymbol>()
                .ToImmutableDictionary(type => type.FullName(), type => type.Locations.Single().SourceTree); //TODO multiple locations

            var outputFiles = compiledAssembly.DefinedTypes
                .GroupBy(type => typeTreeMap[type.FullName])
                .Select(grouping => {
                    var result = GenerateOutput(grouping);
                    var inputFile = trees[grouping.Key];
                    var outputFile = Path.Combine(environment.IntermediateOutputPath, inputFile.Replace(DefaultInputFileEnd, DefaultOutputFileEnd_IntellisenseVisible));//TODO replace end
                    environment.WriteText(outputFile, result);
                    return outputFile;
                })
                .ToImmutableArray();
            return new GeneratorResult(outputFiles, ImmutableArray<GeneratorError>.Empty);
        }

        static string GenerateOutput(IEnumerable<System.Reflection.TypeInfo> types) {
            return types
                .Select(type => {
                    return type.DeclaredMethods
                        .Where(method => method.IsPublic)
                        .Select(method => (string)method.Invoke(null, null))
                        .InsertDelimeter(NewLine);
                })
                .InsertDelimeter(Enumerable.Repeat(NewLine, 2))
                .SelectMany(x => x)
                .ConcatStrings();
        }
    }
    public static class RoslynExtensions {
        public static string FullName(this INamedTypeSymbol type) {
            return type.ContainingNamespace + "." + type.Name;
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
