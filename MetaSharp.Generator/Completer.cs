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
using System.Text;
using System.Threading.Tasks;
using CompleterResult = MetaSharp.Native.Either<System.Collections.Immutable.ImmutableArray<MetaSharp.CompleterError>, string>;

namespace MetaSharp {
    delegate CompleterResult TypeCompleter(SemanticModel model, INamedTypeSymbol type);
    public class CompleterError {
        public readonly SyntaxTree Tree;
        public readonly string Id, Message;
        public readonly FileLinePositionSpan Span;
        public CompleterError(SyntaxTree tree, string id, string message, FileLinePositionSpan span) {
            Tree = tree;
            Span = span;
            Id = id;
            Message = message;
        }
        public CompleterError(SyntaxNode node, string id, string message)
            : this(node.SyntaxTree, id, message, node.LineSpan()) {
        }
    }
    public static class Completer {
        static ImmutableDictionary<Type, TypeCompleter> Completers;
        static Completer() {
            Completers = ImmutableDictionary<Type, TypeCompleter>.Empty
                .Add(typeof(MetaCompleteClassAttribute), ClassCompleter.Generate)
                .Add(typeof(MetaCompleteViewModelAttribute), ViewModelCompleter.Generate)
                .Add(typeof(MetaCompleteDependencyPropertiesAttribute), DependencyPropertiesCompleter.Generate)
            ;
        }
        internal static Either<ImmutableArray<GeneratorError>, ImmutableArray<Output>> GetCompletions(CSharpCompilation compilation, Environment environment) {
            var prototypes = compilation
                .GetAttributeValues<MetaProtoAttribute, Tuple<string, MetaLocationKind>>(values => values.ToValues<string, MetaLocationKind>())
                .Select(x => new {
                    Input = x.Item1,
                    Files = new {
                        Input = Path.GetFullPath(x.Item1),
                        Output = OutputFileName.Create(x.Item1, environment, x.Item2)
                    }
                })
                .ToImmutableDictionary(x => Generator.ParseFile(environment, x.Input), x => x.Files);
            var compilationWithPrototypes = compilation.AddSyntaxTrees(prototypes.Keys);
            var symbolToTypeMap = Completers.Keys.ToImmutableDictionary(
                type => compilationWithPrototypes.GetTypeByMetadataName(type.FullName),
                type => type
            );
            //TODO check syntax errors first
            //TODO use rewriters/rewriting rules from already compiled meta assembly
            //TODO generate errors if class is not partial
            //TODO allow to specify default complete attributes in MetaProto attribute??

            var results = prototypes
                    .Select(pair => {
                        var tree = pair.Key;
                        var model = compilationWithPrototypes.GetSemanticModel(tree);
                        var classSyntaxes = tree.GetRoot().DescendantNodes(x => !(x is ClassDeclarationSyntax)).OfType<ClassDeclarationSyntax>();
                        var treeResults = classSyntaxes
                            .Select(x => {
                                var type = model.GetDeclaredSymbol(x);
                                //TODO support multiple completers for single file
                                var completer = type.GetAttributes()
                                            .Select(attributeData => symbolToTypeMap.GetValueOrDefault(attributeData.AttributeClass))
                                            .Where(attributeType => attributeType != null)
                                            .Select(attributeType => Completers[attributeType])
                                            .SingleOrDefault();
                                if(completer == null)
                                    return null;
                                var context = type.CreateContext();
                                var completion = completer(model, type);
                                return completion.Transform(
                                    errors => errors.Select(e => GeneratorError.Create(id: e.Id, file: prototypes[e.Tree].Input, message: e.Message, span: e.Span)),
                                    value => context.WrapMembers(value)
                                );
                            })
                            .Where(x => x != null);
                        return treeResults
                            .AggregateEither(
                                left => left.SelectMany(x => x), 
                                right => new Output(right.ConcatStringsWithNewLines(), pair.Value.Output)
                            );
                    });
            return results
                .AggregateEither(
                    left => left
                        .SelectMany(x => x)
                        .ToImmutableArray(), 
                    right => right.ToImmutableArray()
                );
        }
        //static Either<ImmutableArray<Com>>
    }
}