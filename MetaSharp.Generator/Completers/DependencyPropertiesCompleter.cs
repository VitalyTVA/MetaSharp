using MetaSharp.Native;
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
    //TODO multiple statements in cctor
    static class DependencyPropertiesCompleter {
        public static string Generate(SemanticModel model, INamedTypeSymbol type) {
            //TODO error or skip if null
            var cctor = type.StaticConstructor();
            var syntax = (ConstructorDeclarationSyntax)cctor.Node();
            var chain = syntax.Body.Statements
                .Select(statement => {
                    return (statement as ExpressionStatementSyntax)?.Expression as InvocationExpressionSyntax;
                })
                .Where(invocation => invocation != null)
                .Select(invocation => LinqExtensions.Unfold(
                        invocation,
                        x => (x.Expression as MemberAccessExpressionSyntax)?.Expression as InvocationExpressionSyntax
                    )
                    .ToArray()
                )
                .Where(x => {
                    var lastMemberAccess = x.Last().Expression as MemberAccessExpressionSyntax;
                    return lastMemberAccess != null;
                })
                .First();
            var last = (MemberAccessExpressionSyntax)chain.Last().Expression;
            var ownerType = ((GenericNameSyntax)last.Expression).TypeArgumentList.Arguments.Single().ToFullString(); //TODO check last name == "New"
            var properties = chain
                .Take(chain.Length - 1)
                .Select(x => {
                    var memberAccess = (MemberAccessExpressionSyntax)x.Expression;
                    var propertyType = ((GenericNameSyntax)memberAccess.Name).TypeArgumentList.Arguments.Single().ToFullString();
                    var propertyName = ((IdentifierNameSyntax)x.ArgumentList.Arguments[1].Expression).ToFullString().ReplaceEnd("Property", string.Empty);
                    return new { propertyType, propertyName };
                })
                .Reverse()
                .ToArray();
            var propertiesString = properties
                .Select(x => {
                    return 
$@"public static readonly DependencyProperty {x.propertyName}Property;
public {x.propertyType} {x.propertyName} {{
    get {{ return ({x.propertyType})GetValue({x.propertyName}Property); }}
    set {{ SetValue({x.propertyName}Property, value); }}
}}
";
                })
                .ConcatStringsWithNewLines();
            var result = 
$@"partial class {type.Name} {{
{propertiesString.AddTabs(1)}
}}";
            return result;
        }
    }
}