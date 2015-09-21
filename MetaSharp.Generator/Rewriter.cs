using MetaSharp.Native;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MetaSharp {
    public static class Rewriter {
        public static ImmutableArray<TreeReplacement> GetReplacements(CSharpCompilation compilation) {
            var replacements = GetReplacementsCore(compilation);
            return replacements
                .GroupBy(x => x.Tree)
                .Select(grouping => {
                    var tree = grouping.Key;
                    var newRoot = tree.GetRoot().ReplaceNodes(grouping.Select(x => x.Old), (x, _) => grouping.Single(y => y.Old == x).New);
                    var newTree = tree.WithRootAndOptions(newRoot, tree.Options);
                    return new TreeReplacement(tree, newTree);
                })
                .ToImmutableArray();
        }
        static ImmutableArray<SyntaxtReplacement> GetReplacementsCore(CSharpCompilation compilation) {
            return compilation.GetErrors()
                .Where(x => x.Id == "CS0246")
                .Select(error => {
                    var location = error.Location;
                    var node = location.SourceTree.GetRoot().FindNode(location.SourceSpan);

                    var methodNameSyntax = node.GetParents().OfType<GenericNameSyntax>().First();
                    var invocationSyntax = node.GetParents().OfType<InvocationExpressionSyntax>().First();
                    var expression = invocationSyntax.Expression as MemberAccessExpressionSyntax;


                    //var newNametoken = SyntaxFactory.Identifier("Class_");
                    var newNameSyntax = (SimpleNameSyntax)SyntaxFactory.ParseName(methodNameSyntax.Identifier.ValueText + "_");
                    var newInvocationSyntax = invocationSyntax.Update(
                        expression.WithName(newNameSyntax),
                        SyntaxFactory.ParseArgumentList($"(\"{node.ToFullString()}\")")
                    //.AddArguments(invocationSyntax.ArgumentList.Arguments.ToArray())
                    );
                    return new SyntaxtReplacement(location.SourceTree, invocationSyntax, newInvocationSyntax);

                    //TODO check syntax errors before rewriting anything
                    //TOTO metadata includes are not in trees dictionary - need rewrite code in includes as well
                    //TOTO multiple errors in one file

                    //var model = compilation.GetSemanticModel(location.SourceTree);
                    //var symbol = model.GetSymbolInfo(methodNameSyntax).Symbol as IMethodSymbol;
                    //var dublerSymbol = symbol.ContainingType.
                    //    GetMembers()
                    //    .OfType<IMethodSymbol>()
                    //    .First(x => x.Name == "Class_");
                }).ToImmutableArray();
        }
    }
    //public class MetaRewriter : CSharpSyntaxRewriter {
    //    public MetaRewriter(CSharpCompilation) {

    //    }

    //    public override SyntaxNode VisitInvocationExpression(InvocationExpressionSyntax node) {
    //        return base.VisitInvocationExpression(node);
    //    }
    //}
    public struct TreeReplacement {
        public readonly SyntaxTree Old, New;
        public TreeReplacement(SyntaxTree old, SyntaxTree @new) {
            Old = old;
            New = @new;
        }
    }
    public struct SyntaxtReplacement {
        public readonly SyntaxTree Tree;
        public readonly SyntaxNode Old, New;
        public SyntaxtReplacement(SyntaxTree tree, SyntaxNode old, SyntaxNode @new) {
            Tree = tree;
            Old = old;
            New = @new;
        }
    }
}
