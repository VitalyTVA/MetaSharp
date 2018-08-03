﻿using MetaSharp.Native;
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
                .Select(property => {
                    var memberAccess = (MemberAccessExpressionSyntax)property.Expression;
                    var methodName = memberAccess.Name.Identifier.ValueText;
                    if(!methodName.StartsWith("Register"))
                        return null;
                    var readOnly = methodName == "RegisterReadOnly" || methodName == "RegisterAttachedReadOnly";
                    var attached = methodName == "RegisterAttached" || methodName == "RegisterAttachedReadOnly";
                    var service = methodName == "RegisterServiceTemplateProperty";
                    var bindableReadOnly = methodName == "RegisterBindableReadOnly";

                    var arguments = property.ArgumentList.Arguments;

                    var propertySignature = (memberAccess.Name as GenericNameSyntax)?.TypeArgumentList.Arguments.Select(x => x.ToString()).ToArray();
                    if(propertySignature == null) {
                        var defaultValueArgument = service ? null : arguments[readOnly || bindableReadOnly ? 3 : 2].Expression;
                        propertySignature = service
                            ? new[] { "DataTemplate" }
                            : model.GetTypeInfo(defaultValueArgument).Type?.DisplayString(model, defaultValueArgument.GetLocation()).With(propertyType =>
                                !attached
                                    ? new string[] { propertyType }
                                    : (arguments[0].Expression as ParenthesizedLambdaExpressionSyntax).With(x => new string[] { x.ParameterList.Parameters.Single().Type.ToString(), propertyType })
                            );
                    }
                    if(propertySignature == null) {
                        var span = memberAccess.Name.LineSpan();
                        return new CompleterError(memberAccess.SyntaxTree, Messages.DependecyProperty_PropertyTypeMissed, new FileLinePositionSpan(string.Empty, span.EndLinePosition, span.EndLinePosition));
                    }

                    var propertyName = GetPropertyName(arguments, attached, readOnly, bindableReadOnly, memberAccess.Name.SyntaxTree);


                    return propertyName.Select(name => {
                        var overridedPropertyVisibility = GetOverridedPropertyVisibility(type, name);
                        return GenerateFields(ownerType.DisplayString(model, memberAccess.GetLocation()), propertySignature[0], name, readOnly, bindableReadOnly, overridedPropertyVisibility == null) + (attached
                        ? GenerateAttachedProperty(propertySignature[0], propertySignature[1], name, readOnly, overridedPropertyVisibility)
                        : GenerateProperty(propertySignature.Single(), name, readOnly, bindableReadOnly, overridedPropertyVisibility));
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
        static Either<CompleterError, string> GetPropertyName(SeparatedSyntaxList<ArgumentSyntax> arguments, bool attached, bool readOnly, bool bindableReadOnly, SyntaxTree tree) {
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

            Func<int, string, string, CompleterError> getError = (index, prefix, suffix) => {
                var fieldName = ((IdentifierNameSyntax)arguments[index].Expression).ToString();
                if(prefix + propertyName + suffix != fieldName) {
                    var message = Messages.DependecyProperty_IncorrectPropertyName.Format(propertyName, prefix + propertyName + suffix);
                    return new CompleterError(arguments[index].Expression, message);
                }
                return null;
            };
            return (getError(1, bindableReadOnly ? "set" : string.Empty, bindableReadOnly ? "" : "Property" + (readOnly ? "Key" : string.Empty)) ?? (bindableReadOnly || readOnly ? getError(2, string.Empty, "Property") : null))
                .Return(x => Either<CompleterError, string>.Left(x), () => propertyName);
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
        static string GenerateProperty(string propertyType, string propertyName, bool readOnly, bool bindableReadOnly, Tuple<MemberVisibility, MemberVisibility, bool> overridedPropertyVisibility) {
            string getterModifier = overridedPropertyVisibility == null ? "public " : overridedPropertyVisibility.Item2.ToCSharp(MemberVisibility.Private);
            string setterModifier = overridedPropertyVisibility == null ? (readOnly ? "private " : "") : overridedPropertyVisibility.Item1.ToCSharp(overridedPropertyVisibility.Item2);
            var nonBrowsable = overridedPropertyVisibility != null && overridedPropertyVisibility.Item3;
            string attributes = nonBrowsable ? "[Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]\r\n" : "";
            string setterAttributes = bindableReadOnly & !nonBrowsable ? "[Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]\r\n    " : string.Empty;
            string keySuffix = readOnly ? "Key" : "";
            return
$@"{attributes}{getterModifier}{propertyType} {propertyName} {{
    get {{ return ({propertyType})GetValue({propertyName}Property); }}
    {setterAttributes}{setterModifier}set {{ SetValue({propertyName}Property{keySuffix}, value); }}
}}
";
        }
        static string GenerateAttachedProperty(string componentType, string propertyType, string propertyName, bool readOnly, Tuple<MemberVisibility, MemberVisibility, bool> overridedPropertyVisibility) {
            string getterModifier = overridedPropertyVisibility == null ? "public " : overridedPropertyVisibility.Item2.ToCSharp(MemberVisibility.Private);
            string setterModifier = overridedPropertyVisibility == null ? (readOnly ? "" : "public ") : overridedPropertyVisibility.Item1.ToCSharp(MemberVisibility.Private);
            string keySuffix = readOnly ? "Key" : "";
            return
$@"{getterModifier}static {propertyType} Get{propertyName}({componentType} d) {{
    return ({propertyType})d.GetValue({propertyName}Property);
}}
{setterModifier}static void Set{propertyName}({componentType} d, {propertyType} value) {{
    d.SetValue({propertyName}Property{keySuffix}, value);
}}
";
        }
    }
}