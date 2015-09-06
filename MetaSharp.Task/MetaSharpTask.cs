using MetaSharp.Utils;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
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

namespace MetaSharp.Tasks {
    public class MetaSharpTask : ITask {
        public IBuildEngine BuildEngine { get; set; }
        public ITaskHost HostObject { get; set; }

        [Required]
        public ITaskItem[] InputFiles { get; set; }
        [Required]
        public string IntermediateOutputPath { get; set; }
        [Output]
        public ITaskItem[] OutputFiles { get; set; }

        public bool Execute() {
            var files = InputFiles
                .Select(x => x.ItemSpec)
                .Where(Generator.IsMetaSharpFile)
                .ToImmutableArray();
            var references = PlatformEnvironment.DefaultReferences;
            var result = Generator.Generate(files, CreateEnvironment(), references);
            if(result.Errors.Any()) {
                foreach(var error in result.Errors) {
                    BuildEngine.LogErrorEvent(ToBuildError(error));
                }
                return false;
            } else {
                OutputFiles = result.Files
                    .Select(x => new TaskItem(x))
                    .ToArray();
                return true;
            }

            //var type = compilation.GlobalNamespace.GetNamespaceMembers().ElementAt(0).GetNamespaceMembers().ElementAt(0).GetTypeMembers().Single();
            //var tree = type.Locations.Single().SourceTree;
            //if(tree == compilation.SyntaxTrees.Single()) {
            //}
            //var node = compilation.SyntaxTrees.Single().GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>().First();
            //var model = compilation.GetSemanticModel(compilation.SyntaxTrees.Single());
            //var symbol = model.GetDeclaredSymbol(node);
            //if(symbol == type) {
            //}
        }
        static BuildErrorEventArgs ToBuildError(GeneratorError error) {
            return new BuildErrorEventArgs(
                        subcategory: "MetaSharp",
                        code: error.Id,
                        file: error.File,
                        lineNumber: error.LineNumber + 1,
                        columnNumber: error.ColumnNumber + 1,
                        endLineNumber: error.EndLineNumber + 1,
                        endColumnNumber: error.EndColumnNumber + 1,
                        message: error.Message,
                        helpKeyword: string.Empty,
                        senderName: "MetaSharp"
                        );
        }
        Environment CreateEnvironment() {
            return PlatformEnvironment.Create(
                readText: fileName => File.ReadAllText(fileName),
                writeText: (fileName, text) => File.WriteAllText(fileName, text),
                intermediateOutputPath: IntermediateOutputPath);
        }
    }
    public static class PlatformEnvironment {
        public static Environment Create(Func<string, string> readText, Action<string, string> writeText, string intermediateOutputPath) {
            return new Environment(
                readText: readText, 
                writeText: writeText,
                loadAssembly: stream => Assembly.Load(stream.GetBuffer()),
                intermediateOutputPath: intermediateOutputPath,
                getAllMethods: type => type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic),
                getAttributes: type => type.GetCustomAttributes());
        }
        public static readonly ImmutableArray<string> DefaultReferences;
        static PlatformEnvironment() {
            var assemblyPath = Path.GetDirectoryName(typeof(object).Assembly.Location);
            DefaultReferences = ImmutableArray.Create(
                Path.Combine(assemblyPath, "mscorlib.dll"),
                Path.Combine(assemblyPath, "System.dll"),
                Path.Combine(assemblyPath, "System.Core.dll"),
                Path.Combine(assemblyPath, "System.Runtime.dll"),
                typeof(MetaContext).Assembly.Location
                );
        }
    }
}
