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
    public static class Completer {
        internal static ImmutableArray<Output> GetCompletions(CSharpCompilation compilation, Environment environment) {
            var prototypes = compilation.GetFiles<MetaProtoAttribute>(environment);
            var compilationWithPrototypes = compilation.AddSyntaxTrees(prototypes.Keys);
            //TODO check syntax errors first

            return prototypes
                .Select(pair => {
                    var tree = pair.Key;
                    var model = compilationWithPrototypes.GetSemanticModel(tree);
                    var classSyntaxes = tree.GetRoot().DescendantNodes(x => !(x is ClassDeclarationSyntax)).OfType<ClassDeclarationSyntax>();
                    var text = classSyntaxes
                        .Select(x => model.GetDeclaredSymbol(x))
                        .Where(x => x.HasAttribute<MetaCompleteClassAttribute>(compilationWithPrototypes))
                        .Select(type => {
                            var properties = type.GetMembers().OfType<IPropertySymbol>();
                            var context = type.Location().CreateContext(type.ContainingNamespace.ToString());
                            var generator = properties.Aggregate(
                                new ClassGenerator_(type.Name, ClassModifiers.Partial, skipProperties: true),
                                (acc, p) => acc.Property(p.Type.Name, p.Name) //TODO use simple type name
                            );
                            return context.WrapMembers(generator.Generate());
                        }).ConcatStringsWithNewLines();
                    return new Output(text, OutputFileName.Create(pair.Value, environment, MetaLocationKind.IntermediateOutput)); //TODO specify output destination (g.i.cs, etc.)
                })
                .ToImmutableArray();
        }
    }
}