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
using CompleterResult = MetaSharp.Either<System.Collections.Immutable.ImmutableArray<MetaSharp.CompleterError>, string>;

namespace MetaSharp {
    static class ClassCompleter {
        public static CompleterResult Generate(SemanticModel model, INamedTypeSymbol type) {
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
}