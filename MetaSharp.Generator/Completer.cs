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
    static class ClassCompleter {
        public static string Generate(SemanticModel model, INamedTypeSymbol type) {
            var properties = type.Properties();
            var generator = properties.Aggregate(
                new ClassGenerator_(type.Name, ClassModifiers.Partial, skipProperties: true),
                (acc, property) => {
                    var typeName = property.TypeDisplayString(model);
                    return acc.Property(typeName, property.Name);
                }
            );
            return generator.Generate();
        }
    }
    static class ViewModelCompleter {
//TODO auto calc dependent properties
//TODO auto generate default private ctor if none
//TODO error if existing ctor not private
        public static string Generate(SemanticModel model, INamedTypeSymbol type) {
            var properties = type.Properties()
                .Select(p => {
                    return 
$@"public override {p.TypeDisplayString(model)} {p.Name} {{
    get {{ return base.{p.Name}; }}
    set {{
        if(base.{p.Name} == value)
            return;
        base.{p.Name} = value;
        RaisePropertyChanged(""{p.Name}"");
    }}
}}";
                })
                .ConcatStringsWithNewLines();
            return
//TODO what if System.ComponentModel is already in context?
$@"using System.ComponentModel;
partial class {type.Name} {{
    class {type.Name}Implementation : {type.Name}, INotifyPropertyChanged {{
{properties.AddTabs(2)}
        public event PropertyChangedEventHandler PropertyChanged;
        void RaisePropertyChanged(string property) {{
            var handler = PropertyChanged;
            if(handler != null)
                handler(this, new PropertyChangedEventArgs(property));
        }}
    }}
}}";
        }
    }
    delegate string TypeCompleter(SemanticModel model, INamedTypeSymbol type);
    public static class Completer {
        static ImmutableDictionary<Type, TypeCompleter> Completers;
        static Completer() {
            Completers = ImmutableDictionary<Type, TypeCompleter>.Empty
                .Add(typeof(MetaCompleteClassAttribute), ClassCompleter.Generate)
                .Add(typeof(MetaCompleteViewModelAttribute), ViewModelCompleter.Generate)
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