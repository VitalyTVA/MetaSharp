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
    delegate string TypeCompleter(SemanticModel model, INamedTypeSymbol type);
    public static class Completer {
        static ImmutableDictionary<Type, TypeCompleter> Completers;
        static Completer() {
            Completers = ImmutableDictionary<Type, TypeCompleter>.Empty
                .Add(typeof(MetaCompleteClassAttribute), ClassCompleter.Generate)
                .Add(typeof(MetaCompleteViewModelAttribute), ViewModelCompleter.Generate)
                .Add(typeof(MetaCompleteDependencyPropertiesAttribute), DependencyPropertiesCompleter.Generate)
            ;
        }
        internal static ImmutableArray<Output> GetCompletions(CSharpCompilation compilation, Environment environment) {
            var prototypes = compilation
                .GetAttributeValues<MetaProtoAttribute, Tuple<string, MetaLocationKind>>(values => values.ToValues<string, MetaLocationKind>())
                .Select(x => new { Input = x.Item1, Output = OutputFileName.Create(x.Item1, environment, x.Item2) })
                .ToImmutableDictionary(x => Generator.ParseFile(environment, x.Input), x => x.Output);
            var compilationWithPrototypes = compilation.AddSyntaxTrees(prototypes.Keys);
            var symbolToTypeMap = Completers.Keys.ToImmutableDictionary(
                type => compilationWithPrototypes.GetTypeByMetadataName(type.FullName),
                type => type
            );
            //TODO check syntax errors first
            //TODO use rewriters/rewriting rules from already compiled meta assembly
            //TODO generate errors if class is not partial

            return prototypes
                .Select(pair => {
                    var tree = pair.Key;
                    var model = compilationWithPrototypes.GetSemanticModel(tree);
                    var classSyntaxes = tree.GetRoot().DescendantNodes(x => !(x is ClassDeclarationSyntax)).OfType<ClassDeclarationSyntax>();
                    var text = classSyntaxes
                        .Select(x => model.GetDeclaredSymbol(x))
                        .Select(type => {
                            //TODO support multiple completers for single file
                            var completer = type.GetAttributes()
                                .Select(attributeData => symbolToTypeMap.GetValueOrDefault(attributeData.AttributeClass))
                                .Where(attributeType => attributeType != null)
                                .Select(attributeType => Completers[attributeType])
                                .SingleOrDefault();
                            return new { type, completer };
                        })
                        .Where(x => x.completer != null)
                        .Select(x => {
                            var context = x.type.CreateContext();
                            var completion = x.completer(model, x.type);
                            return context.WrapMembers(completion);
                        }).ConcatStringsWithNewLines();
                    return new Output(text, pair.Value);
                })
                .ToImmutableArray();
        }
    }
}