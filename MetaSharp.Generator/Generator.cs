﻿#define TEST
#define TEST
using MetaSharp.Utils;
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
    //TODO methods with arguments
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
        class MethodId {
            public readonly string Name, Type;
            public MethodId(string name, string type) {
                Name = name;
                Type = type;
            }
            public override bool Equals(object obj) {
                var other = obj as MethodId;
                return other != null && other.Name == Name && other.Type == Type;
            }
            public override int GetHashCode() {
                return Name.GetHashCode() ^ Type.GetHashCode();
            }
        }

        const string DefaultSuffix = "meta";
        const string CShaprFileExtension = ".cs";
        const string DefaultInputFileEnd = DefaultSuffix + CShaprFileExtension;
        const string DefaultOutputFileEnd = DefaultSuffix + ".g.i" + CShaprFileExtension;
        const string DefaultOutputFileEnd_IntellisenseInvisible = DefaultSuffix + ".g" + CShaprFileExtension;
        const string DesignerOutputFileEnd = DefaultSuffix + ".designer" + CShaprFileExtension;

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
                options: new CSharpCompilationOptions(
                    OutputKind.DynamicallyLinkedLibrary,
                    assemblyIdentityComparer: DesktopAssemblyIdentityComparer.Default
                ),
                syntaxTrees: trees.Keys
            );

            var errors = compilation.GetDiagnostics()
                .Where(x => x.Severity == DiagnosticSeverity.Error)
                .Select(error => {
                    var span = error.Location.GetLineSpan();
                    return error.ToGeneratorError(
                        file: trees[error.Location.SourceTree],
                        span: error.Location.GetLineSpan());
                })
                .ToImmutableArray();
            if(errors.Any())
                return new GeneratorResult(ImmutableArray<string>.Empty, errors);
            Assembly compiledAssembly;
            using(var stream = new MemoryStream()) {
                var compileResult = compilation.Emit(stream);
                compiledAssembly = environment.LoadAssembly(stream);
            }

            var methodsMap = compilation
                .GetSymbolsWithName(name => true, SymbolFilter.Member)
                .Where(member => member.Kind == SymbolKind.Method && !member.IsImplicitlyDeclared)
                .Cast<IMethodSymbol>()
                .ToImmutableDictionary(
                    method => new MethodId(method.Name, method.ContainingType.FullName()),
                    method => method
                );

            var outputFiles = compiledAssembly.DefinedTypes
                .SelectMany(type => environment.GetAllMethods(type.AsType()).Where(method => (method.IsPublic || method.IsAssembly) && !method.IsSpecialName))
                .GroupBy(method => methodsMap[GetMethodId(method)].Location().SourceTree)
                .SelectMany(grouping => {
                    var methods = grouping
                        .Select(method => new {
                            Method = method,
                            Symbol = methodsMap[GetMethodId(method)]
                        })
                        .OrderBy(info => info.Symbol.Location().GetLineSpan().StartLinePosition)
                        .Select(info => {
                            var location = info.Symbol.Location();
                            var nodes = location.SourceTree.GetCompilationUnitRoot().DescendantNodes(location.SourceSpan);
                            var namespaces = nodes.OfType<NamespaceDeclarationSyntax>().Single(); //TODO nested namespaces
                                var usings = namespaces.Usings.Select(x => x.ToString()).ToArray();
                            return new MethodContext(info.Method, new MetaContext(info.Method.DeclaringType.Namespace, usings));
                        })
                        .ToImmutableArray();
                    var outputs = GenerateOutputs(methods, trees[grouping.Key], environment);
                    outputs.ForEach(output => environment.WriteText(output.FileName, output.Text));
                    return outputs.Select(output => output.FileName);
                })
                .ToImmutableArray();
            return new GeneratorResult(outputFiles, ImmutableArray<GeneratorError>.Empty);
        }

        static MethodId GetMethodId(MethodInfo method) {
            return new MethodId(method.Name, method.DeclaringType.FullName);
        }

        static string GenerateOutput(ImmutableArray<MethodContext> methods) {
            return methods
                .GroupBy(methodContext => methodContext.Method.DeclaringType)
                .Select(grouping => {
                    return grouping
                        .Select(methodContext => {
                            //TODO check args
                            var parameters = methodContext.Method.GetParameters().Length == 1
                                ? methodContext.Context.YieldToArray()
                                : null;
                            return (string)methodContext.Method.Invoke(null, parameters);
                        })
                        .InsertDelimeter(NewLine);
                })
            .InsertDelimeter(Enumerable.Repeat(NewLine, 2))
            .SelectMany(x => x)
            .ConcatStrings();
        }
        static ImmutableArray<Output> GenerateOutputs(ImmutableArray<MethodContext> methods, string inputFileName, Environment environment) {
            return methods
                .GroupBy(method => GetOutputFileName(method.Method.DeclaringType, inputFileName, environment))
                .Select(byOutputGrouping => {
                    var output = byOutputGrouping
                        .GroupBy(methodContext => methodContext.Method.DeclaringType)
                        .Select(grouping => {
                            return grouping
                                .Select(methodContext => {
                                    //TODO check args
                                    var parameters = methodContext.Method.GetParameters().Length == 1
                                        ? methodContext.Context.YieldToArray()
                                        : null;
                                    return (string)methodContext.Method.Invoke(null, parameters);
                                })
                                .InsertDelimeter(NewLine);
                        })
                    .InsertDelimeter(Enumerable.Repeat(NewLine, 2))
                    .SelectMany(x => x)
                    .ConcatStrings();
                    return new Output(output, byOutputGrouping.Key);
                })
                .ToImmutableArray();
        }
        static string GetOutputFileName(Type type, string fileName, Environment environment) {
            var location = environment.GetAttributes(type).OfType<MetaLocationAttribute>().SingleOrDefault()?.Location ?? default(MetaLocationKind);
            return GetOutputFileName(location, fileName, environment);
        }
        static string GetOutputFileName(MetaLocationKind location, string fileName, Environment environment) {
            switch(location) {
            case MetaLocationKind.IntermediateOutput:
                return Path.Combine(environment.IntermediateOutputPath, fileName.ReplaceEnd(DefaultInputFileEnd, DefaultOutputFileEnd));
            case MetaLocationKind.IntermediateOutputNoIntellisense:
                return Path.Combine(environment.IntermediateOutputPath, fileName.ReplaceEnd(DefaultInputFileEnd, DefaultOutputFileEnd_IntellisenseInvisible));
            case MetaLocationKind.Designer:
                return fileName.ReplaceEnd(DefaultInputFileEnd, DesignerOutputFileEnd);
            default:
                throw new InvalidOperationException();
            }
        }
        static GeneratorError ToGeneratorError(this Diagnostic error, string file, FileLinePositionSpan span) {
            return new GeneratorError(
                                    id: error.Id,
                                    file: file,
                                    message: error.GetMessage(),
                                    lineNumber: span.StartLinePosition.Line,
                                    columnNumber: span.StartLinePosition.Character,
                                    endLineNumber: span.EndLinePosition.Line,
                                    endColumnNumber: span.EndLinePosition.Character
                                    );
        }
    }
    public class Output {
        public readonly string Text, FileName;
        public Output(string text, string fileName) {
            Text = text;
            FileName = fileName;
        }
    }
    public class MethodContext {
        public readonly MetaContext Context;
        public readonly MethodInfo Method;
        public MethodContext(MethodInfo method, MetaContext context) {
            Context = context;
            Method = method;
        }
    }
    public static class RoslynExtensions {
        public static string FullName(this INamedTypeSymbol type) {
            return type.ContainingNamespace + "." + type.Name;
        }
        public static Location Location(this IMethodSymbol method) {
            return method.Locations.Single();
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
        public readonly Func<Type, IEnumerable<MethodInfo>> GetAllMethods;
        public readonly Func<Type, IEnumerable<Attribute>> GetAttributes;
        public readonly string IntermediateOutputPath; 
        public Environment(Func<string, string> readText, Action<string, string> writeText, Func<MemoryStream, Assembly> loadAssembly, string intermediateOutputPath, Func<Type, IEnumerable<MethodInfo>> getAllMethods, Func<Type, IEnumerable<Attribute>> getAttributes) {
            ReadText = readText;
            WriteText = writeText;
            LoadAssembly = loadAssembly;
            IntermediateOutputPath = intermediateOutputPath;
            GetAllMethods = getAllMethods;
            GetAttributes = getAttributes;
        }
    }
}