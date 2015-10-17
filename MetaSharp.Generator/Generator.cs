﻿#define TEST
#define TEST
using MetaSharp.Native;
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
using GeneratorResult = MetaSharp.Native.Either<System.Collections.Immutable.ImmutableArray<MetaSharp.GeneratorError>, System.Collections.Immutable.ImmutableArray<string>>;

namespace MetaSharp {
    //TODO different exception messages for console and task mode (no need to format task message at all - simply use to string)
    //TODO wrap output classes into regions
    //TODO show verbose error in correct location when reference or include file or etc. is not found + (incorrect enum type name in MetaReference)
    //TODO Conditional("METASHARP") attribute for all meta attributes so they do not go to final assembly
    //TODO push active configuration/compile constants (DEBUG, etc.) to meta code to make same evnironment in meta code as in main
    //TODO isolate exceptions in generator methods
    //TODO non static classes
    //TODO methods with arguments
    //TODO other environment constants (OutDir, etc.) - use in meta context/meta attributes
    //TODO explicit file name mode
    //TODO automatically generate namespace and usings based on usings above and under namespace
    //TODO generate stub types
    //TODO reference other assemblies from predefined locations
    //TODO use SourceText with SyntaxFactory
    //TODO use SourceReferenceResolver?
    //TODO option to insert delimeters between output from different classes and methods
    //TODO debugging
    //TODO save rewritten files to disk to show errors from them and for debugging
    //TODO return ClassGenerator and other generators directly from methods

    //TODO recursive includes and references
    //TODO duplicate includes and references
    //TODO invalid includes and references

    //TODO ADT, immutable objects, DProps, ViewModels, MonadTransfomers, Templates, Localization, Aspects, Pattern Matching (+for enums), 
	//TODO Mix-in/default interface implementations (IBindingList as example)
    //TODO Wrap object in viewmodels, expose entity properties from view model as bindable
    //TODO parsers
    //TODO binary output - drawing images??

    //TODO single usage extension + diagnostic, Unit<T> + only Unit diagnostic
    //TODO diagnostic - always name parameters with same type, always name default parameters, specify named parameters in correct order etc.
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

        public static readonly ImmutableArray<PortableExecutableReference> DefaultReferences;
        static readonly string FrameworkPath = Path.GetDirectoryName(typeof(object).Assembly.Location);
        static Generator() {
            DefaultReferences = new[] {
                Path.Combine(FrameworkPath, "mscorlib.dll"),
                Path.Combine(FrameworkPath, "System.dll"),
                Path.Combine(FrameworkPath, "System.Core.dll"),
                Path.Combine(FrameworkPath, "System.Runtime.dll"),
                typeof(MetaContext).Assembly.Location,
            }
            .Select(x => MetadataReference.CreateFromFile(x))
            .ToImmutableArray();
        }
        const string DefaultSuffix = "meta";
        public const string CShaprFileExtension = ".cs";
        public const string DefaultInputFileEnd = DefaultSuffix + CShaprFileExtension;
        public const string DefaultOutputFileEnd = ".g.i" + CShaprFileExtension;
        public const string DefaultOutputFileEnd_IntellisenseInvisible = ".g" + CShaprFileExtension;
        public const string DesignerOutputFileEnd = ".designer" + CShaprFileExtension;

        const string DefaultAssemblyName = "meta.dll";
        const string NewLine = "\r\n";
        const string ConditionalConstant = "METASHARP";

        static readonly CSharpParseOptions ParseOptions = CSharpParseOptions.Default.WithPreprocessorSymbols(ConditionalConstant);

        public static bool IsMetaSharpFile(string fileName) {
            return fileName.EndsWith(DefaultInputFileEnd);
        }

        public static GeneratorResult Generate(ImmutableArray<string> files, Environment environment) {
            var trees = files.ToImmutableDictionary(file => ParseFile(environment, file), file => file);


            var compilation = CSharpCompilation.Create(
                DefaultAssemblyName,
                references: DefaultReferences,
                options: new CSharpCompilationOptions(
                    OutputKind.DynamicallyLinkedLibrary,
                    assemblyIdentityComparer: DesktopAssemblyIdentityComparer.Default
                ),
                syntaxTrees: trees.Keys
            );
            var metaReferences = compilation.GetMetaReferences(environment.BuildConstants);
            compilation = compilation
                .AddSyntaxTrees(
                    compilation
                        .GetAttributeValues<MetaIncludeAttribute, string>(values => values.ToValue<string>())
                        .Select(x => ParseFile(environment, x))
                )
                .AddReferences(metaReferences.Values.Select(x => MetadataReference.CreateFromFile(x)));

            var replacements = Rewriter.GetReplacements(compilation, trees.Keys);
            replacements
                .ForEach(replacement => {
                    var tree = replacement.Old;
                    compilation = compilation.ReplaceSyntaxTree(tree, replacement.New);
                    var oldFile = trees[tree];
                    trees = trees.Remove(tree).Add(replacement.New, oldFile);
                });

            var errors = compilation.GetErrors()
                .Select(error => {
                    var span = error.Location.GetLineSpan();
                    return error.ToGeneratorError(
                        file: trees[error.Location.SourceTree],
                        span: error.Location.GetLineSpan());
                })
                .ToImmutableArray();

            if(errors.Any())
                return Error(errors);
            Assembly compiledAssembly;
            using(var stream = new MemoryStream()) {
                var compileResult = compilation.Emit(stream);
                compiledAssembly = Assembly.Load(stream.GetBuffer());
            }

            //TODO ignore nested methods
            //TODO support overloaded methods
            var metaContextType = compilation.GetType<MetaContext>();
            var methodsMap = compilation
                .GetSymbolsWithName(name => true, SymbolFilter.Member)
                .Where(member => member.Kind == SymbolKind.Method && !member.IsImplicitlyDeclared)
                .Cast<IMethodSymbol>()
                .Where(method => {
                    return !method.Parameters.Any() 
                    || (method.Parameters.Length == 1 
                        && method.Parameters.Single().Type == metaContextType
                        && method.Parameters.Single().RefKind == RefKind.None);
                })
                .Where(method => trees.ContainsKey(method.Location().SourceTree) && !method.TypeParameters.Any())
                .ToImmutableDictionary(
                    method => new MethodId(method.Name, method.ContainingType.FullName()),
                    method => method
                );
            ResolveEventHandler resolveHandler = (o, e) => {
                var name = new AssemblyName(e.Name);
                var fileName = metaReferences.GetValueOrDefault(name.Name);
                return fileName != null ? Assembly.LoadFrom(fileName) : null;
            };
            AppDomain.CurrentDomain.AssemblyResolve += resolveHandler;
            try {
                var outputs = compiledAssembly.GetTypes()
                    .SelectMany(type => type
                        .GetMethods(BindingFlags.Static | BindingFlags.Public)
                        .Where(method => !method.IsSpecialName) //TODO filter out methods which do not return something useful (or even fail if there are such)
                    )
                    .Where(method => methodsMap.ContainsKey(GetMethodId(method)))
                    .Select(method => { 
                        var location = methodsMap[GetMethodId(method)].Location(); //TODO use main location
                        var context = location.CreateContext(method.DeclaringType.Namespace);
                        var tree = methodsMap[GetMethodId(method)].Location().SourceTree;
                        var methodContext = new MethodContext(method, context, location.GetLineSpan(), trees[tree]);
                        return GetMethodOutput(methodContext)
                            .Select(x => new Output(x, GetOutputFileName(methodContext.Method, methodContext.FileName, environment)));
                    })
                    .AggregateEither(
                        e => e.ToImmutableArray(),
                        values => values
                            .GroupBy(x => x.FileName)
                            .Select(x => new Output(x.Select(output => output.Text).ConcatStringsWithNewLines(), x.Key))
                            .ToImmutableArray()
                    );

                if(outputs.IsLeft())
                    return Error(outputs.ToLeft());

                var completerResult = Completer.GetCompletions(compilation, environment);
                if(completerResult.IsLeft())
                    return Error(completerResult.ToLeft());
                var completions = completerResult.ToRight();
                var outputFiles = outputs.ToRight().Concat(completions)
                    .Select(output => { 
                        environment.WriteText(output.FileName.FileName, output.Text);
                        return output;
                    })
                    .Where(output => output.FileName.IncludeInOutput)
                    .Select(output => output.FileName.FileName)
                    .ToImmutableArray();
                return Success(outputFiles);
            } finally {
                AppDomain.CurrentDomain.AssemblyResolve -= resolveHandler;
            }
        }
        static GeneratorResult Success(ImmutableArray<string> files) {
            return GeneratorResult.Right(files);
        }
        static GeneratorResult Error(ImmutableArray<GeneratorError> errors) {
            return GeneratorResult.Left(errors);
        }
        static ImmutableDictionary<string, string> GetMetaReferences(this CSharpCompilation compilation, BuildConstants buildConsants) {
            return compilation
                .GetAttributeValues<MetaReferenceAttribute, Tuple<string, ReferenceRelativeLocation>>(values => values.ToValues<string, ReferenceRelativeLocation>())
                .Select(values => {
                    var path = values.Item1;
                    var location = values.Item2;
                    var relativePath = GetReferenceRelativePath(buildConsants, location);
                    return Path.Combine(relativePath, path);
                })
                .ToImmutableDictionary(x => Path.GetFileNameWithoutExtension(x), x => x);
        }
        static string GetReferenceRelativePath(BuildConstants buildConsants, ReferenceRelativeLocation location) {
            switch(location) {
            case ReferenceRelativeLocation.Project:
                return string.Empty;
            case ReferenceRelativeLocation.TargetPath:
                return buildConsants.TargetPath;
            case ReferenceRelativeLocation.Framework:
                return FrameworkPath;
            default:
                throw new InvalidOperationException();
            }
        }

        internal static SyntaxTree ParseFile(Environment environment, string x) {
            return SyntaxFactory.ParseSyntaxTree(environment.ReadText(x), ParseOptions);
        }

        static MethodId GetMethodId(MethodInfo method) {
            return new MethodId(method.Name, method.DeclaringType.FullName);
        }
        static Either<GeneratorError, string> GetMethodOutput(MethodContext methodContext) {
            //TODO check args
            var parameters = methodContext.Method.GetParameters().Length == 1
                ? methodContext.Context.YieldToArray()
                : null;
            try {
                var methodResult = methodContext.Method.Invoke(null, parameters);
                if(methodContext.Method.ReturnType == typeof(string)) {
                    return Either<GeneratorError, string>.Right((string)methodResult);
                } else {
                    return Either<GeneratorError, string>.Right(((IEnumerable<string>)methodContext.Method.Invoke(null, parameters)).ConcatStringsWithNewLines());
                }
            } catch(TargetInvocationException e) {
                var error = GeneratorError.Create(Messages.Exception_Id, methodContext.FileName, string.Format(Messages.Exception_Message, e.InnerException.Message, e.InnerException), methodContext.MethodSpan);
                return Either<GeneratorError, string>.Left(error);
            }
        }

        static OutputFileName GetOutputFileName(MethodInfo method, string fileName, Environment environment) {
            var location = method.GetCustomAttribute<MetaLocationAttribute>()?.Location
                ?? method.DeclaringType.GetCustomAttribute<MetaLocationAttribute>()?.Location
                ?? default(MetaLocationKind);
            return OutputFileName.Create(fileName, environment, location);
        }
        static GeneratorError ToGeneratorError(this Diagnostic error, string file, FileLinePositionSpan span) {
            return GeneratorError.Create(id: error.Id,
                                    file: file,
                                    message: error.GetMessage(),
                                    span: span
                                    );
        }
    }


    public enum GeneratorResultCode {
        Success = 0,
        Error = 1,
    }
    public static class RealEnvironmentGenerator {
        public static GeneratorResultCode Generate(
            ImmutableArray<string> files, 
            BuildConstants buildConstants, 
            Action<GeneratorError> reportError, 
            Action<ImmutableArray<string>> reportOutputFiles) {

            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
            try {
                var result = Generator.Generate(files, CreateEnvironment(buildConstants));
                return result.Match(
                    errors => {
                        foreach(var error in errors) {
                            reportError(error);
                        }
                        return GeneratorResultCode.Error;
                    },
                    outputFiles => {
                        reportOutputFiles(outputFiles);
                        return GeneratorResultCode.Success;
                    }
                );
            } finally {
                AppDomain.CurrentDomain.AssemblyResolve -= CurrentDomain_AssemblyResolve;
            }
        }
        static Environment CreateEnvironment(BuildConstants buildConstants) {
            return new Environment(
                readText: fileName => File.ReadAllText(fileName),
                writeText: (fileName, text) => File.WriteAllText(fileName, text),
                buildConstants: buildConstants);
        }
        static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args) {
            if(args.Name == typeof(MetaContext).Assembly.FullName)
                return typeof(MetaContext).Assembly;
            return null;
        }
    }

    class Output {
        public readonly string Text;
        public readonly OutputFileName FileName;
        public Output(string text, OutputFileName fileName) {
            Text = text;
            FileName = fileName;
        }
    }
    class OutputFileName {
        public static OutputFileName Create(string fileName, Environment environment, MetaLocationKind location) {
            return new OutputFileName(GetOutputFileName(location, fileName, environment), location != MetaLocationKind.Designer);
        }
        static string GetOutputFileName(MetaLocationKind location, string fileName, Environment environment) {
            switch(location) {
            case MetaLocationKind.IntermediateOutput:
                return Path.Combine(environment.BuildConstants.IntermediateOutputPath, fileName.ReplaceEnd(Generator.CShaprFileExtension, Generator.DefaultOutputFileEnd));
            case MetaLocationKind.IntermediateOutputNoIntellisense:
                return Path.Combine(environment.BuildConstants.IntermediateOutputPath, fileName.ReplaceEnd(Generator.CShaprFileExtension, Generator.DefaultOutputFileEnd_IntellisenseInvisible));
            case MetaLocationKind.Designer:
                return fileName.ReplaceEnd(Generator.CShaprFileExtension, Generator.DesignerOutputFileEnd);
            default:
                throw new InvalidOperationException();
            }
        }


        public readonly string FileName;
        public readonly bool IncludeInOutput;

        OutputFileName(string fileName, bool includeInOutput) {
            FileName = fileName;
            IncludeInOutput = includeInOutput;
        }
        public override int GetHashCode() {
            return FileName.GetHashCode() ^ IncludeInOutput.GetHashCode();
        }
        public override bool Equals(object obj) {
            var other = obj as OutputFileName;
            return other != null && other.FileName == FileName && other.IncludeInOutput == IncludeInOutput;
        }
    }
    public class MethodContext {
        public readonly MetaContext Context;
        public readonly MethodInfo Method;
        public readonly FileLinePositionSpan MethodSpan;
        public readonly string FileName;
        public MethodContext(MethodInfo method, MetaContext context, FileLinePositionSpan methodSpan, string fileName) {
            Context = context;
            Method = method;
            MethodSpan = methodSpan;
            FileName = fileName;
        }
    }
    public static class RoslynExtensions {
        public static string FullName(this INamedTypeSymbol type) {
            return type.ContainingNamespace + "." + type.Name;
        }
        public static Location Location(this ISymbol method) {
            return method.Locations.Single();
        }
        public static IEnumerable<TOutput> GetAttributeValues<TAttribute, TOutput>(this CSharpCompilation compilation, Func<object[], TOutput> getValue) where TAttribute : Attribute {
            var attributeSymbol = compilation.GetType<TAttribute>();
            return compilation.Assembly.GetAttributes()
                .Where(attribute => attribute.AttributeClass == attributeSymbol)
                .Select(attribute => getValue(attribute.ConstructorArguments.Select(x => x.Value).ToArray()));
        }
        public static bool HasAttribute<T>(this ISymbol symbol, Compilation compilation) where T : Attribute {
            var attributeSymbol = compilation.GetType<T>();
            return symbol.GetAttributes().Any(x => x.AttributeClass == attributeSymbol);
        }

        public static INamedTypeSymbol GetType<T>(this Compilation compilation) {
            return compilation.GetTypeByMetadataName(typeof(T).FullName);
        }

        public static IEnumerable<Diagnostic> GetErrors(this CSharpCompilation compilation) {
            return compilation.GetDiagnostics().Where(x => x.Severity == DiagnosticSeverity.Error);
        }
        public static IEnumerable<SyntaxNode> GetParents(this SyntaxNode node) {
            return LinqExtensions.Unfold(node.Parent, x => x.Parent);
        }
        public static MetaContext CreateContext(this Location location, string @namespace) {
            var nodes = location.SourceTree.GetCompilationUnitRoot().DescendantNodes(location.SourceSpan);
            var namespaces = nodes.OfType<NamespaceDeclarationSyntax>().Single(); //TODO nested namespaces
            return new MetaContext(@namespace, namespaces.Usings.Select(x => x.ToString()).ToArray());
        }
        public static MetaContext CreateContext(this INamedTypeSymbol type) {
            return type.Location().CreateContext(type.ContainingNamespace.ToString());
        }
        public static string TypeDisplayString(this IPropertySymbol property, SemanticModel model) {
            return property.Type.DisplayString(model, property.Location());
        }
        public static string DisplayString(this ITypeSymbol type, SemanticModel model, Location location) {
            return type.ToMinimalDisplayString(model, location.SourceSpan.Start, SymbolDisplayFormat.FullyQualifiedFormat);
        }
        public static IEnumerable<IPropertySymbol> Properties(this INamedTypeSymbol type) {
            return type.GetMembers().OfType<IPropertySymbol>();
        }
        public static IEnumerable<IMethodSymbol> Methods(this INamedTypeSymbol type) {
            return type.GetMembers().OfType<IMethodSymbol>();
        }
        public static IMethodSymbol StaticConstructor(this INamedTypeSymbol type) {
            return type.Methods().FirstOrDefault(x => x.Name == WellKnownMemberNames.StaticConstructorName);
        }
        public static string ToAccessibilityModifier(this Accessibility accessibility, Accessibility? containingAccessibility) {
            if(containingAccessibility != null && containingAccessibility.Value == accessibility)
                return string.Empty;
            switch(accessibility) {
            case Accessibility.Private:
                return string.Empty;
            case Accessibility.Protected:
                return "protected ";
            case Accessibility.Internal:
                return "internal ";
            case Accessibility.ProtectedOrInternal:
                return "protected internal ";
            case Accessibility.Public:
                return "public ";
            default:
                throw new InvalidOperationException();
            }
        }
        public static bool IsAutoImplemented(this IPropertySymbol property) {
            var propertySyntax = (PropertyDeclarationSyntax)property.Node();
            return propertySyntax.AccessorList.Accessors.First().Body == null;
        }
        public static SyntaxNode Node(this ISymbol symbol) {
            var location = symbol.Location();
            return location.SourceTree.GetCompilationUnitRoot().FindNode(location.SourceSpan);
        }
        public static FileLinePositionSpan LineSpan(this SyntaxNode syntax) {
            return syntax.GetLocation().GetLineSpan();
        }
    }
    public class GeneratorError {
        public static GeneratorError Create(string id, string file, string message, FileLinePositionSpan span) {
            return new GeneratorError(
                        id: id,
                        file: file,
                        message: message,
                        lineNumber: span.StartLinePosition.Line + 1,
                        columnNumber: span.StartLinePosition.Character + 1,
                        endLineNumber: span.EndLinePosition.Line + 1,
                        endColumnNumber: span.EndLinePosition.Character + 1
                        );
        }
        public readonly string Id, File, Message;
        public readonly int LineNumber, ColumnNumber, EndLineNumber, EndColumnNumber;
        GeneratorError(string id, string file, string message, int lineNumber, int columnNumber, int endLineNumber, int endColumnNumber) {
            Id = id;
            File = file;
            Message = message;
            LineNumber = lineNumber;
            ColumnNumber = columnNumber;
            EndLineNumber = endLineNumber;
            EndColumnNumber = endColumnNumber;
        }

        public override string ToString() {
            return $"{File}({LineNumber},{ColumnNumber},{EndLineNumber},{EndColumnNumber}): error {Id}: {Message}";
        }
    }
    public class Environment {
        public readonly Func<string, string> ReadText;
        public readonly Action<string, string> WriteText;
        public readonly BuildConstants BuildConstants; 
        public Environment(
            Func<string, string> readText, 
            Action<string, string> writeText,
            BuildConstants buildConstants) {
            ReadText = readText;
            WriteText = writeText;
            BuildConstants = buildConstants;
        }
    }
    public class BuildConstants {
        public readonly string IntermediateOutputPath, TargetPath;
        public BuildConstants(string intermediateOutputPath, string targetPath) {
            IntermediateOutputPath = intermediateOutputPath;
            TargetPath = targetPath;
        }
    }
}
