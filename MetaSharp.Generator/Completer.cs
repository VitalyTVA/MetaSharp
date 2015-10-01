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
    public interface ICompleter {
        string Generate(SemanticModel model, INamedTypeSymbol type);
    }
    public class ClassCompleter : ICompleter {
        string ICompleter.Generate(SemanticModel model, INamedTypeSymbol type) {
            var properties = type.GetMembers().OfType<IPropertySymbol>();
            var generator = properties.Aggregate(
                new ClassGenerator_(type.Name, ClassModifiers.Partial, skipProperties: true),
                (acc, p) => {
                    var typeName = p.Type.ToMinimalDisplayString(model, p.Location().SourceSpan.Start, SymbolDisplayFormat.FullyQualifiedFormat);
                    return acc.Property(typeName, p.Name);
                }
            );
            return generator.Generate();
        }
    }
    public class ViewModelCompleter : ICompleter {
        string ICompleter.Generate(SemanticModel model, INamedTypeSymbol type) {
            throw new NotImplementedException();
        }
    }
    public static class Completer {
        static ImmutableDictionary<Type, ICompleter> Completers;
        static Completer() {
            Completers = ImmutableDictionary<Type, ICompleter>.Empty
                .Add(typeof(MetaCompleteClassAttribute), new ClassCompleter())
                .Add(typeof(MetaCompleteViewModelAttribute), new ViewModelCompleter())
            ;
        }
        internal static ImmutableArray<Output> GetCompletions(CSharpCompilation compilation, Environment environment) {
            var prototypes = compilation
                .GetAttributeValues<MetaProtoAttribute, Tuple<string, MetaLocationKind>>(values => values.ToValues<string, MetaLocationKind>())
                .Select(x => new { Input = x.Item1, Output = OutputFileName.Create(x.Item1, environment, x.Item2) })
                .ToImmutableDictionary(x => Generator.ParseFile(environment, x.Input), x => x.Output);
            var compilationWithPrototypes = compilation.AddSyntaxTrees(prototypes.Keys);
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
                        .Where(x => x.HasAttribute<MetaCompleteClassAttribute>(compilationWithPrototypes))
                        .Select(type => {
                            var context = type.Location().CreateContext(type.ContainingNamespace.ToString());

                            var completion = Completers[typeof(MetaCompleteClassAttribute)].Generate(model, type);

                            return context.WrapMembers(completion);
                        }).ConcatStringsWithNewLines();
                    return new Output(text, pair.Value);
                })
                .ToImmutableArray();
        }
    }
}