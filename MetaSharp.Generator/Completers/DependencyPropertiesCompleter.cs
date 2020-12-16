using MetaSharp.Native;
using MetaSharp.Utils;
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
    //TODO output errors when too few parameters
    public static class DependencyPropertiesCompleter {

        public static CompleterResult Generate(SemanticModel model, INamedTypeSymbol type) {
            //TODO error or skip if null
            var cctor = type.StaticConstructor();
            if(cctor == null || cctor.IsImplicitlyDeclared)
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
$@"partial class {type.ToString().Split('.').Last()} {{
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
                .Zip(default(InvocationExpressionSyntax).Yield().Concat(chain), (property, attributes) => {
                    var memberAccess = (MemberAccessExpressionSyntax)property.Expression;
                    var methodName = memberAccess.Name.Identifier.ValueText;
                    var arguments = property.ArgumentList.Arguments;

                    if(!methodName.StartsWith("Register") && (methodName != "AddOwner" || (arguments.Count <= 3 && !(memberAccess.Name is GenericNameSyntax)))) // AddOwner completion requires default value argument
                        return null;

                    var attributesMemberAccess = (MemberAccessExpressionSyntax)attributes?.Expression;
                    var attributesArguments = attributesMemberAccess?.Name.Identifier.ValueText == "Attributes" ? attributes?.ArgumentList.Arguments : null;

                    return new { property, memberAccess, methodName, arguments, attributes = attributesArguments ?? new SeparatedSyntaxList<ArgumentSyntax>() };
                })
                .Where(x => x != null)
                .Select(p => {
                    var addOwner = p.methodName == "AddOwner";
                    var readOnly = p.methodName == "RegisterReadOnly" || p.methodName == "RegisterAttachedReadOnly";
                    var attached = p.methodName == "RegisterAttached" || p.methodName == "RegisterAttachedReadOnly";
                    var service = p.methodName == "RegisterServiceTemplateProperty";
                    var bindableReadOnly = p.methodName == "RegisterBindableReadOnly";

                    var propertySignature = (p.memberAccess.Name as GenericNameSyntax)?.TypeArgumentList.Arguments.Select(x => x.ToString()).ToArray();
                    if(propertySignature == null) {
                        var defaultValueArgument = service ? null : GetDefaultValueArgument(p.arguments, addOwner, readOnly, bindableReadOnly);
                        propertySignature = service
                            ? new[] { "DataTemplate" }
                            : model.GetTypeInfo(defaultValueArgument).Type?.DisplayString(model, defaultValueArgument.GetLocation()).With(propertyType =>
                                !attached
                                    ? new string[] { propertyType }
                                    : (p.arguments[0].Expression as ParenthesizedLambdaExpressionSyntax).With(x => new string[] { x.ParameterList.Parameters.Single().Type.ToString(), propertyType })
                            );
                    }
                    if(propertySignature == null) {
                        var span = p.memberAccess.Name.LineSpan();
                        return new CompleterError(p.memberAccess.SyntaxTree, Messages.DependecyProperty_PropertyTypeMissed, new FileLinePositionSpan(string.Empty, span.EndLinePosition, span.EndLinePosition));
                    }

                    var propertyName = GetPropertyName(p.arguments, addOwner, attached, readOnly, bindableReadOnly, p.memberAccess.Name.SyntaxTree);


                    return propertyName.Select(name => {
                        var overridedPropertyVisibility = GetOverridedPropertyVisibility(type, name);
                        return GenerateFields(ownerType.DisplayString(model, p.memberAccess.GetLocation()), propertySignature[0], name, readOnly, bindableReadOnly, overridedPropertyVisibility == null) + (attached
                        ? GenerateAttachedProperty(propertySignature[0], propertySignature[1], name, readOnly, overridedPropertyVisibility)
                        : GenerateProperty(type.ToDisplayString(), propertySignature.Single(), name, readOnly, bindableReadOnly, overridedPropertyVisibility, p.attributes));
                    });
                })
                .Where(x => x != null)
                .Reverse()
                .ToArray();
            return properties
                .AggregateEither(errors => errors.ToImmutableArray(), values => values.ConcatStringsWithNewLines());
        }
        static Tuple<MemberVisibility, MemberVisibility, bool> GetOverridedPropertyVisibility(INamedTypeSymbol type, string propertyName) {
            var field = type.GetMembers(propertyName + "Property");
            if(field.Length != 1) return null;
            var attr = field[0].GetAttributes().Where(x => x.AttributeClass.Name == "DPAccessModifierAttribute" || x.AttributeClass.Name == "DPAccessModifier").FirstOrDefault();
            if(attr == null) return null;
            var setterVisibility = attr.ConstructorArguments.Length < 1 ? MemberVisibility.Public : (MemberVisibility)Enum.Parse(typeof(MemberVisibility), attr.ConstructorArguments[0].Value.ToString());
            var getterVisibility = attr.ConstructorArguments.Length < 2 ? MemberVisibility.Public : (MemberVisibility)Enum.Parse(typeof(MemberVisibility), attr.ConstructorArguments[1].Value.ToString());
            var nonBrowsable = attr.ConstructorArguments.Length < 3 ? false : bool.Parse(attr.ConstructorArguments[2].Value.ToString());
            foreach(var parameter in attr.NamedArguments) {
                switch(parameter.Key) {
                case "SetterVisibility":
                    setterVisibility = (MemberVisibility)Enum.Parse(typeof(MemberVisibility), parameter.Value.Value.ToString());
                    break;
                case "GetterVisibility":
                    getterVisibility = (MemberVisibility)Enum.Parse(typeof(MemberVisibility), parameter.Value.Value.ToString());
                    break;
                case "NonBrowsable":
                    nonBrowsable = bool.Parse(parameter.Value.Value.ToString());
                    break;
                default:
                    throw new InvalidOperationException();
                }
            }
            return Tuple.Create(setterVisibility, getterVisibility, nonBrowsable);
        }
        static Either<CompleterError, string> GetPropertyName(SeparatedSyntaxList<ArgumentSyntax> arguments, bool addOwner, bool attached, bool readOnly, bool bindableReadOnly, SyntaxTree tree) {
            string propertyName;
            var expression = arguments[0].Expression;
            var propertyNameResult = GetPropertyName(expression);
            if(propertyNameResult.IsLeft())
                return propertyNameResult;
            propertyName = propertyNameResult.ToRight();
            if(attached) {
                var getterName = propertyName;
                if(getterName.Length <= "Get".Length || !getterName.StartsWith("Get", StringComparison.Ordinal)) {
                    var message = Messages.DependecyProperty_IncorrectAttachedPropertyGetterName.Format(getterName);
                    return new CompleterError(expression, message);
                }
                propertyName = getterName.Substring("Get".Length);
            } else if(addOwner)
                propertyName = propertyName.Replace("Property", string.Empty);

            Func<int, string, string, CompleterError> getError = (index, prefix, suffix) => {
                var fieldName = ((IdentifierNameSyntax)arguments[index].Expression).ToString();
                if(prefix + propertyName + suffix != fieldName) {
                    var message = Messages.DependecyProperty_IncorrectPropertyName.Format(propertyName, prefix + propertyName + suffix);
                    return new CompleterError(arguments[index].Expression, message);
                }
                return null;
            };
            var idx = addOwner && arguments[0].Expression is IdentifierNameSyntax ? 0 : 1;
            return (getError(idx, bindableReadOnly ? "set" : string.Empty, bindableReadOnly ? "" : "Property" + (readOnly ? "Key" : string.Empty)) ?? (bindableReadOnly || readOnly ? getError(2, string.Empty, "Property") : null))
                .Return(x => Either<CompleterError, string>.Left(x), () => propertyName);
        }
        static Either<CompleterError, string> GetPropertyName(ExpressionSyntax expression) {
            switch(expression) {
                case SimpleLambdaExpressionSyntax lambda:
                    return lambda.Body is MemberAccessExpressionSyntax memberExpresion
                        ? memberExpresion.Name.ToString()
                        : ((InvocationExpressionSyntax)lambda.Body).Expression.ToString();
                case LambdaExpressionSyntax lambdaExpression:
                    return ((InvocationExpressionSyntax)lambdaExpression.Body).Expression.ToString();
                case IdentifierNameSyntax id:
                    return id.Identifier.ValueText;
                case LiteralExpressionSyntax literalExpression:
                    return literalExpression.Token.ValueText;
                case InvocationExpressionSyntax invocationExpression:
                    if (invocationExpression.ArgumentList.Arguments.Count > 0)
                        return invocationExpression.ArgumentList.Arguments[0].Expression.ToString();
                    break;
            }
            return new CompleterError(expression.SyntaxTree, Messages.DependecyProperty_UnsupportedSyntax, expression.LineSpan());
        }
        static ExpressionSyntax GetDefaultValueArgument(SeparatedSyntaxList<ArgumentSyntax> arguments, bool addOwner, bool readOnly, bool bindableReadOnly) {
            var idx = addOwner || readOnly || bindableReadOnly ? 3 : 2;
            if(addOwner && arguments[0].Expression is IdentifierNameSyntax)
                idx = 2;
            return arguments[idx].Expression;
        }
        static string GenerateFields(string ownerType, string propertyType, string propertyName, bool readOnly, bool bindableReadOnly, bool generatePropertyField) {
            string propertyField =
                generatePropertyField
?
$@"public static readonly DependencyProperty {propertyName}Property;" + System.Environment.NewLine
:
"";
            if(readOnly) {
                return propertyField +
$@"static readonly DependencyPropertyKey {propertyName}PropertyKey;" + System.Environment.NewLine;
            }
            if(bindableReadOnly) {
                return propertyField +
$@"static readonly Action<{ownerType}, {propertyType}> set{propertyName};" + System.Environment.NewLine;
            }
            return propertyField;
        }
        static string GenerateProperty(string typeFullName, string propertyType, string propertyName, bool readOnly, bool bindableReadOnly, Tuple<MemberVisibility, MemberVisibility, bool> overridedPropertyVisibility, SeparatedSyntaxList<ArgumentSyntax> attributes) {
            string getterModifier = overridedPropertyVisibility == null ? "public " : overridedPropertyVisibility.Item2.ToCSharp(MemberVisibility.Private);
            string setterModifier = overridedPropertyVisibility == null ? (readOnly ? "private " : "") : overridedPropertyVisibility.Item1.ToCSharp(overridedPropertyVisibility.Item2);
            var nonBrowsable = overridedPropertyVisibility != null && overridedPropertyVisibility.Item3;
            var withDXDescription = attributes.Any(x => x.ToString() == "DXDescription");
            string attributesString =
                (nonBrowsable ? "[Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]\r\n" : "") +
                (withDXDescription ? $"[DXDescription(\"{typeFullName},{propertyName}\")]\r\n" : "") +
                string.Concat(attributes.Select(attribute => {
                    if(attribute.ToString() == "DXDescription") return null;
                    var attributeExpression = attribute.Expression as InvocationExpressionSyntax;
                    if(attributeExpression == null) return null;
                    return "[" + attributeExpression.ToString() + "]\r\n";
                }));
            string setterAttributes = bindableReadOnly && !nonBrowsable ? "[Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]\r\n    " : string.Empty;
            string keySuffix = readOnly ? "Key" : "";
            return
$@"{attributesString}{getterModifier}{propertyType} {propertyName} {{
    get {{ return ({propertyType})GetValue({propertyName}Property); }}
    {setterAttributes}{setterModifier}set {{ SetValue({propertyName}Property{keySuffix}, value); }}
}}
";
        }
        static string GenerateAttachedProperty(string componentType, string propertyType, string propertyName, bool readOnly, Tuple<MemberVisibility, MemberVisibility, bool> overridedPropertyVisibility) {
            string getterModifier = overridedPropertyVisibility == null ? "public " : overridedPropertyVisibility.Item2.ToCSharp(MemberVisibility.Private);
            string setterModifier = overridedPropertyVisibility == null ? (readOnly ? "" : "public ") : overridedPropertyVisibility.Item1.ToCSharp(MemberVisibility.Private);
            string keySuffix = readOnly ? "Key" : "";
            var nonBrowsable = overridedPropertyVisibility != null && overridedPropertyVisibility.Item3;
            string attributes = nonBrowsable ? "[Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]\r\n" : "";
            string setterAttributes = setterModifier != "public " ? "" : attributes;
            return
$@"{attributes}{getterModifier}static {propertyType} Get{propertyName}({componentType} d) {{
    return ({propertyType})d.GetValue({propertyName}Property);
}}
{setterAttributes}{setterModifier}static void Set{propertyName}({componentType} d, {propertyType} value) {{
    d.SetValue({propertyName}Property{keySuffix}, value);
}}
";
        }
    }
}