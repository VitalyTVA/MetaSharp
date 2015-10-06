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
            var buildConstants = new BuildConstants(
                intermediateOutputPath: IntermediateOutputPath, 
                targetPath: OutDir
            );
            var code = RealEnvironmentGenerator.Generate(
                files,
                buildConstants,
                error => BuildEngine.LogErrorEvent(ToBuildError(error)),
                output => OutputFiles = output.Select(x => new TaskItem(x)).ToArray()
             );
            return code == GeneratorResultCode.Success;

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
                        lineNumber: error.LineNumber,
                        columnNumber: error.ColumnNumber,
                        endLineNumber: error.EndLineNumber,
                        endColumnNumber: error.EndColumnNumber,
                        message: error.Message,
                        helpKeyword: string.Empty,
                        senderName: "MetaSharp"
                        );
        }
    }
}
