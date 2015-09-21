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
            return compilation.GetErrors()
                .Where(x => x.Id == "CS0246")
                .GroupBy(x => x.Location.SourceTree)
                .Select(group => {
                    var tree = group.Key;
                    var root = tree.GetRoot();
                    var errorNodes = group
                        .Select(x => root.FindNode(x.Location.SourceSpan))
                        .ToImmutableArray();
                    var rewriter = new MetaRewriter(errorNodes);
                    var newRoot = rewriter.Visit(root);
                    var newTree = tree.WithRootAndOptions(newRoot, tree.Options);
                    return new TreeReplacement(tree, newTree);
                })
                .ToImmutableArray();
        }
    }
    public class MetaRewriter : CSharpSyntaxRewriter {
        readonly ImmutableArray<SyntaxNode> errorNodes;
        public MetaRewriter(ImmutableArray<SyntaxNode> errorNodes) {
            this.errorNodes = errorNodes;
        }

        public override SyntaxNode VisitInvocationExpression(InvocationExpressionSyntax invocationSyntax) {
            var errorNode = errorNodes.FirstOrDefault(x => Equals(x.GetParents().OfType<InvocationExpressionSyntax>().First(), invocationSyntax));
            if(errorNode == null)
                return base.VisitInvocationExpression(invocationSyntax);

            var methodNameSyntax = errorNode.GetParents().OfType<GenericNameSyntax>().First();
            var expression = invocationSyntax.Expression as MemberAccessExpressionSyntax;


            //var newNametoken = SyntaxFactory.Identifier("Class_");
            var newNameSyntax = (SimpleNameSyntax)SyntaxFactory.ParseName(methodNameSyntax.Identifier.ValueText + "_");
            var newInvocationSyntax = invocationSyntax.Update(
                expression
                    .WithName(newNameSyntax)
                    .WithExpression((ExpressionSyntax)Visit(expression.Expression)),
                SyntaxFactory.ParseArgumentList($"(\"{errorNode.ToFullString()}\")")
            //.AddArguments(invocationSyntax.ArgumentList.Arguments.ToArray())
            );

            //TODO check syntax errors before rewriting anything
            //TOTO metadata includes are not in trees dictionary - need rewrite code in includes as well
            //TOTO multiple errors in one file

            //var model = compilation.GetSemanticModel(location.SourceTree);
            //var symbol = model.GetSymbolInfo(methodNameSyntax).Symbol as IMethodSymbol;
            //var dublerSymbol = symbol.ContainingType.
            //    GetMembers()
            //    .OfType<IMethodSymbol>()
            //    .First(x => x.Name == "Class_");

            return newInvocationSyntax;
        }
    }
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
