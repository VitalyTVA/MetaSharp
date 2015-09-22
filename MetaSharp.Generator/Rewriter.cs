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
                    var rewriter = new MetaRewriter(compilation.GetSemanticModel(tree), errorNodes);
                    var newRoot = rewriter.Visit(root);
                    var newTree = tree.WithRootAndOptions(newRoot, tree.Options);
                    return new TreeReplacement(tree, newTree);
                })
                .ToImmutableArray();
        }
    }
    public class MetaRewriter : CSharpSyntaxRewriter {
        readonly ImmutableArray<SyntaxNode> errorNodes;
        readonly SemanticModel model;
        public MetaRewriter(SemanticModel model, ImmutableArray<SyntaxNode> errorNodes) {
            this.model = model;
            this.errorNodes = errorNodes;
        }

        public override SyntaxNode VisitInvocationExpression(InvocationExpressionSyntax invocationSyntax) {
            var errorNode = errorNodes.FirstOrDefault(x => Equals(x.GetParents().OfType<InvocationExpressionSyntax>().First(), invocationSyntax));
            if(errorNode == null)
                return base.VisitInvocationExpression(invocationSyntax);

            var methodNameSyntax = errorNode.GetParents().OfType<GenericNameSyntax>().FirstOrDefault();
            //TODO check semantic if methodNameSyntax
            if(methodNameSyntax == null)
                return base.VisitInvocationExpression(invocationSyntax);
            var expression = invocationSyntax.Expression as MemberAccessExpressionSyntax;


            var newNameSyntax = (SimpleNameSyntax)SyntaxFactory.ParseName(methodNameSyntax.Identifier.ValueText + "_");
            var newArguments = ((ArgumentListSyntax)VisitArgumentList(invocationSyntax.ArgumentList)).Arguments.ToArray();
            var newInvocationSyntax = invocationSyntax.Update(
                expression
                    .WithName(newNameSyntax)
                    .WithExpression((ExpressionSyntax)Visit(expression.Expression)),
                SyntaxFactory.ParseArgumentList($"(\"{errorNode.ToFullString()}\")")
                    .AddArguments(newArguments)
            );

            //TODO type name is alias (using Foo = Bla.Bla.Doo);
            //TODO check syntax errors before rewriting anything
            //TODO metadata includes are not in trees dictionary - need rewrite code in includes as well
            //TODO multiple errors in one file
            //TODO rewrite explicit generator type (ClassGenerator g = ...; g.Property ...)

            //var model = compilation.GetSemanticModel(location.SourceTree);
            //var symbol = model.GetSymbolInfo(methodNameSyntax).Symbol as IMethodSymbol;
            //var dublerSymbol = symbol.ContainingType.
            //    GetMembers()
            //    .OfType<IMethodSymbol>()
            //    .First(x => x.Name == "Class_");

            return newInvocationSyntax;
        }
        public override SyntaxNode VisitSimpleLambdaExpression(SimpleLambdaExpressionSyntax lambda) {
            return VisitLambdaExpression(lambda);
        }
        public override SyntaxNode VisitParenthesizedLambdaExpression(ParenthesizedLambdaExpressionSyntax lambda) {
            return VisitLambdaExpression(lambda);
        }

        SyntaxNode VisitLambdaExpression(LambdaExpressionSyntax lambda) {
            //TODO check parents and semantic of parents
            //var parents = lambda.GetParents();
            //var argument = lambda.Parent;
            //var symbol = model.GetSymbolInfo(argument.Parent.Parent).Symbol as IMethodSymbol;

            //TODO check lambda expression parameters (1 parameter, type is same as method's generic parameter)
            //TODO custom args order (named parameters)
            //TODO works only for 'Property' methods
            //TODO semantic model checks

            var body = (MemberAccessExpressionSyntax)lambda.Body;
            var property = body.Name.Identifier.Text;
            var literal = SyntaxFactory.Literal(property);
            return SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, literal);
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
