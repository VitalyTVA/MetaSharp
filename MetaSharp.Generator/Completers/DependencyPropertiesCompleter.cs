using MetaSharp.Native;
using MetaSharp.Utils;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CompleterResult = MetaSharp.Native.Either<System.Collections.Immutable.ImmutableArray<MetaSharp.CompleterError>, string>;

namespace MetaSharp {
    //TODO report invalid owner type error
    //TODO report invalid dependency property [key] field name error
    //TODO report property type specified error
    //TODO multiple statements in cctor
    //TODO AddOwner support
    //TODO determine expression type based on default value??
    //TODO output errors when too few parameters
    public static class DependencyPropertiesCompleter {
        const string ErrorPrefix = "M#";
        //TODO check all messages
        public const string PropertyTypeMissed_Id = ErrorPrefix + "0001";
        public const string PropertyTypeMissed_Message = "Either property type should be explicitly specified or default value should be explicitly typed to generate dependency property";
        public const string IncorrectPropertyName_Id = ErrorPrefix + "0002";
        public const string IncorrectPropertyName_Message = "Dependency property field for the the property '{0}' should have '{1}' name.";

        public static CompleterResult Generate(SemanticModel model, INamedTypeSymbol type) {
            //TODO error or skip if null
            var cctor = type.StaticConstructor();
            var syntax = (ConstructorDeclarationSyntax)cctor.Node();
            var properties = syntax.Body.Statements
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
                .Where(chain => {
                    var lastMemberAccess = chain.Last().Expression as MemberAccessExpressionSyntax;
                    return (lastMemberAccess?.Expression as GenericNameSyntax)?.Identifier.ValueText == "DependencyPropertyRegistrator" //TODO check real type from model
                        && lastMemberAccess?.Name.Identifier.ValueText == "New";
                })
                .Select(chain => GenerateProperties(model, type, chain));
            return properties
                .AggregateEither(
                    errors => errors.SelectMany(x => x).ToImmutableArray(),
                    values => values.ConcatStringsWithNewLines())
                .Select(values =>
$@"partial class {type.Name} {{
{values.AddTabs(1)}
}}");
        }

        static CompleterResult GenerateProperties(SemanticModel model, INamedTypeSymbol type, InvocationExpressionSyntax[] chain) {
            var last = (MemberAccessExpressionSyntax)chain.Last().Expression;
            var ownerType = ((GenericNameSyntax)last.Expression).TypeArgumentList.Arguments.Single().ToString(); //TODO check last name == "New"
            var properties = chain
                .Take(chain.Length - 1)
                .Select(x => {
                    var memberAccess = (MemberAccessExpressionSyntax)x.Expression;
                    var methodName = memberAccess.Name.Identifier.ValueText;
                    if(!methodName.StartsWith("Register"))
                        return null;
                    var readOnly = methodName == "RegisterReadOnly" || methodName == "RegisterAttachedReadOnly";
                    var attached = methodName == "RegisterAttached" || methodName == "RegisterAttachedReadOnly";
                    
                    var arguments = x.ArgumentList.Arguments;


                    var propertyType = (memberAccess.Name as GenericNameSyntax)?.TypeArgumentList.Arguments.Single().ToString();
                    if(propertyType == null) {
                        var defaultValueArgument = arguments[readOnly ? 3 : 2].Expression;
                        var defaultExpressionTypeInfo = model.GetTypeInfo(defaultValueArgument);
                        propertyType = defaultExpressionTypeInfo.Type?.DisplayString(model, defaultValueArgument.GetLocation());
                    }
                    if(propertyType == null) {
                        var span = memberAccess.Name.GetLocation().GetLineSpan();
                        return Either<CompleterError, string>.Left(new CompleterError(memberAccess.Name.SyntaxTree, PropertyTypeMissed_Id, PropertyTypeMissed_Message, new FileLinePositionSpan(string.Empty, span.EndLinePosition, span.EndLinePosition)));
                    }

                    var fieldName = ((IdentifierNameSyntax)arguments[1].Expression).ToString();
                    var propertyName = ((MemberAccessExpressionSyntax)((SimpleLambdaExpressionSyntax)arguments[0].Expression).Body).Name.ToString();
                    if(propertyName + "Property" + (readOnly ? "Key" : string.Empty) != fieldName) {
                        var message = string.Format(IncorrectPropertyName_Message, propertyName, propertyName + "Property");
                        return Either<CompleterError, string>.Left(new CompleterError(memberAccess.Name.SyntaxTree, IncorrectPropertyName_Id, message, arguments[1].Expression.GetLocation().GetLineSpan()));
                    }
                    var text = GenerateFields(propertyName, readOnly) + System.Environment.NewLine + (attached
                        ? GenerateAttachedProperty(propertyType, propertyName, readOnly)
                        : GenerateProperty(propertyType, propertyName, readOnly));
                    return Either<CompleterError, string>.Right(text);
                })
                .Where(x => x != null)
                .Reverse()
                .ToArray();
            return properties
                .AggregateEither(errors => errors.ToImmutableArray(), values => values.ConcatStringsWithNewLines());
        }
        static string GenerateFields(string propertyName, bool readOnly) {
            return readOnly
?
$@"public static readonly DependencyProperty {propertyName}Property;
static readonly DependencyPropertyKey {propertyName}PropertyKey;"
:
$@"public static readonly DependencyProperty {propertyName}Property;";
        }
        static string GenerateProperty(string propertyType, string propertyName, bool readOnly) {
            return readOnly
?
$@"public {propertyType} {propertyName} {{
    get {{ return ({propertyType})GetValue({propertyName}Property); }}
    private set {{ SetValue({propertyName}PropertyKey, value); }}
}}
"
:
$@"public {propertyType} {propertyName} {{
    get {{ return ({propertyType})GetValue({propertyName}Property); }}
    set {{ SetValue({propertyName}Property, value); }}
}}
";
        }
        static string GenerateAttachedProperty(string propertyType, string propertyName, bool readOnly) {
            return readOnly
?
$@"public {propertyType} Get{propertyName}(DependencyObject d) {{
    return ({propertyType})d.GetValue({propertyName}Property);
}}
void Set{propertyName}(DependencyObject d, {propertyType} value) {{
    d.SetValue({propertyName}PropertyKey, value);
}}
"
:
$@"public {propertyType} Get{propertyName}(DependencyObject d) {{
    return ({propertyType})d.GetValue({propertyName}Property);
}}
public void Set{propertyName}(DependencyObject d, {propertyType} value) {{
    d.SetValue({propertyName}Property, value);
}}
";
        }
    }
}