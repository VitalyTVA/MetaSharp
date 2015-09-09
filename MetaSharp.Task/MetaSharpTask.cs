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
        [Required]
        public string OutDir { get; set; }
        [Output]
        public ITaskItem[] OutputFiles { get; set; }

        public bool Execute() {
            var files = InputFiles
                .Select(x => x.ItemSpec)
                .Where(Generator.IsMetaSharpFile)
                .ToImmutableArray();
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
            GeneratorResult result;
            try {
                result = Generator.Generate(files, CreateEnvironment());
            } finally {
                AppDomain.CurrentDomain.AssemblyResolve -= CurrentDomain_AssemblyResolve;
            }
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

        private Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args) {
            if(args.Name == typeof(MetaContext).Assembly.FullName)
                return typeof(MetaContext).Assembly;
            return null;
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
            return new Environment(
                readText: fileName => File.ReadAllText(fileName),
                writeText: (fileName, text) => File.WriteAllText(fileName, text),
                intermediateOutputPath: IntermediateOutputPath);
        }
    }
}
