using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MetaSharp {
    //TODO report invalid owner type error
    //TODO report invalid dependency property field name error
    //TODO report property type specified error
    static class DependencyPropertiesCompleter {
        public static string Generate(SemanticModel model, INamedTypeSymbol type) {
            var cctor = type.StaticConstructor();
            //TODO error or skip if null
            var syntax = (ConstructorDeclarationSyntax)cctor.Node();
            //var regStatement = syntax.Body.Statements.OfType()
            return string.Empty;
        }
    }
}