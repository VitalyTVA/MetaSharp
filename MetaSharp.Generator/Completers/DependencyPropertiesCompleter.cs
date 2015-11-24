using MetaSharp.Native;
using MetaSharp.Utils;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CompleterResult = MetaSharp.Either<System.Collections.Immutable.ImmutableArray<MetaSharp.CompleterError>, string>;

namespace MetaSharp {
    //TODO AddOwner support
    //TODO output errors when too few parameters
    public static class DependencyPropertiesCompleter {

        public static CompleterResult Generate(SemanticModel model, INamedTypeSymbol type) {
            //TODO error or skip if null
            var cctor = type.StaticConstructor();
            if(cctor == null)
                return default(string);
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
                    values => {
                        var conctenated = values.ConcatStringsWithNewLines();
                        if(string.IsNullOrEmpty(conctenated))
                            return null;
                        return
$@"partial class {type.Name} {{
{conctenated.AddTabs(1)}
}}";
                    });
        }

        static CompleterResult GenerateProperties(SemanticModel model, INamedTypeSymbol type, InvocationExpressionSyntax[] chain) {
            var last = (MemberAccessExpressionSyntax)chain.Last().Expression;
            var ownerTypeSyntax = ((GenericNameSyntax)last.Expression).TypeArgumentList.Arguments.Single();
            var ownerType = model.GetTypeInfo(ownerTypeSyntax).Type;
            if(ownerType != type)
                return new CompleterError(ownerTypeSyntax, Messages.DependecyProperty_IncorrectOwnerType).YieldToImmutable();
            var properties = chain
                .Take(chain.Length - 1)
                .Select(property => {
                    var memberAccess = (MemberAccessExpressionSyntax)property.Expression;
                    var methodName = memberAccess.Name.Identifier.ValueText;
                    if(!methodName.StartsWith("Register"))
                        return null;
                    var readOnly = methodName == "RegisterReadOnly" || methodName == "RegisterAttachedReadOnly";
                    var attached = methodName == "RegisterAttached" || methodName == "RegisterAttachedReadOnly";

                    var arguments = property.ArgumentList.Arguments;

                    var propertySignature = (memberAccess.Name as GenericNameSyntax)?.TypeArgumentList.Arguments.Select(x => x.ToString()).ToArray();
                    if(propertySignature == null) {
                        var defaultValueArgument = arguments[readOnly ? 3 : 2].Expression;
                        var defaultExpressionTypeInfo = model.GetTypeInfo(defaultValueArgument);
                        propertySignature = defaultExpressionTypeInfo.Type?.DisplayString(model, defaultValueArgument.GetLocation()).With(propertyType =>
                            !attached
                                ? new string[] { propertyType }
                                : (arguments[0].Expression as ParenthesizedLambdaExpressionSyntax).With(x => new string[] { x.ParameterList.Parameters.Single().Type.ToString(), propertyType })
                        );
                    }
                    if(propertySignature == null) {
                        var span = memberAccess.Name.LineSpan();
                        return new CompleterError(memberAccess.SyntaxTree, Messages.DependecyProperty_PropertyTypeMissed, new FileLinePositionSpan(string.Empty, span.EndLinePosition, span.EndLinePosition));
                    }

                    var propertyName = GetPropertyName(arguments, attached, readOnly, memberAccess.Name.SyntaxTree);

                    return propertyName.Select(name => {
                        return GenerateFields(name, readOnly) + System.Environment.NewLine + (attached
                        ? GenerateAttachedProperty(propertySignature[0], propertySignature[1], name, readOnly)
                        : GenerateProperty(propertySignature.Single(), name, readOnly));
                    });
                })
                .Where(x => x != null)
                .Reverse()
                .ToArray();
            return properties
                .AggregateEither(errors => errors.ToImmutableArray(), values => values.ConcatStringsWithNewLines());
        }

        static Either<CompleterError, string> GetPropertyName(SeparatedSyntaxList<ArgumentSyntax> arguments, bool attached, bool readOnly, SyntaxTree tree) {
            string propertyName;
            if(!attached) {
                propertyName = ((MemberAccessExpressionSyntax)((SimpleLambdaExpressionSyntax)arguments[0].Expression).Body).Name.ToString();
            } else {
                var getterName = ((InvocationExpressionSyntax)((LambdaExpressionSyntax)arguments[0].Expression).Body).Expression.ToString();
                if(getterName.Length <= "Get".Length || !getterName.StartsWith("Get", StringComparison.Ordinal)) {
                    var message = Messages.DependecyProperty_IncorrectAttachedPropertyGetterName.Format(getterName);
                    return new CompleterError(arguments[0].Expression, message);
                }
                propertyName = getterName.Substring("Get".Length);
            }

            Func<int, string, CompleterError> getError = (index, suffix) => {
                var fieldName = ((IdentifierNameSyntax)arguments[index].Expression).ToString();
                if(propertyName + suffix != fieldName) {
                    var message = Messages.DependecyProperty_IncorrectPropertyName.Format(propertyName, propertyName + suffix);
                    return new CompleterError(arguments[index].Expression, message);
                }
                return null;
            };
            return (getError(1, "Property" + (readOnly ? "Key" : string.Empty)) ?? (readOnly ? getError(2, "Property") : null))
                .Return(x => Either<CompleterError, string>.Left(x), () => propertyName);
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
        static string GenerateAttachedProperty(string componentType, string propertyType, string propertyName, bool readOnly) {
            return readOnly
?
$@"public static {propertyType} Get{propertyName}({componentType} d) {{
    return ({propertyType})d.GetValue({propertyName}Property);
}}
static void Set{propertyName}({componentType} d, {propertyType} value) {{
    d.SetValue({propertyName}PropertyKey, value);
}}
"
:
$@"public static {propertyType} Get{propertyName}({componentType} d) {{
    return ({propertyType})d.GetValue({propertyName}Property);
}}
public static void Set{propertyName}({componentType} d, {propertyType} value) {{
    d.SetValue({propertyName}Property, value);
}}
";
        }
    }
}