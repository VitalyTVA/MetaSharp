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
    public static class ViewModelCompleter {
        public static readonly Func<string, string> Implemetations = typeName =>
$@"public event PropertyChangedEventHandler PropertyChanged;
void RaisePropertyChanged(string property) {{
    var handler = PropertyChanged;
    if(handler != null)
        handler(this, new PropertyChangedEventArgs(property));
}}
void RaisePropertyChanged<T>(System.Linq.Expressions.Expression<Func<{typeName}, T>> property) {{
    RaisePropertyChanged(DevExpress.Mvvm.Native.ExpressionHelper.GetPropertyName(property));
}}
object parentViewModel;
object DevExpress.Mvvm.ISupportParentViewModel.ParentViewModel {{
    get {{ return parentViewModel; }}
    set {{
        if(parentViewModel == value)
            return;
        var oldParentViewModel = parentViewModel;
        parentViewModel = value;
        OnParentViewModelChanged(oldParentViewModel);
    }}
}}
partial void OnParentViewModelChanged(object oldParentViewModel);".AddTabs(1);

        public static readonly string Attrubutes = //TODO do not add this stub if Mvvm is already referenced via MetaReference?? (can't find how to write test for it)
@"
using System;
namespace DevExpress.Mvvm.DataAnnotations {
    public class BindablePropertyAttribute : Attribute {
        public BindablePropertyAttribute()
            : this(true) {
        }
        public BindablePropertyAttribute(bool isBindable) {
            this.IsBindable = isBindable;
        }
        public bool IsBindable { get; private set; }
        public string OnPropertyChangedMethodName { get; set; }
        public string OnPropertyChangingMethodName { get; set; }
    }
}
";
        //TODO generate typed parent viewmode if view model has TParent view model parameter
        //TODO auto calc dependent properties
        //TODO auto generate default private ctor if none, use explicit factory methods directly
        //TODO error if base class ctor is used
        //TODO error if existing ctor not private
        //TODO implement INPC in class, not in inherited class, so you can call RaisePropertyChanged without extension methods
        //TODO INotifyPropertyChanging support
        //struct BindableAttribute {
        //    readonly bool IsBindable;
        //    readonly string OnPropertyChangedPropertyName;
        //}

        class BindableInfo { //TODO make struct, auto-completed (self-hosting)
            public readonly bool IsBindable;
            public readonly string OnPropertyChangedMethodName, OnPropertyChangingMethodName;
            public BindableInfo(bool isBindable, string onPropertyChangedMethodName, string onPropertyChangingMethodName) {
                IsBindable = isBindable;
                OnPropertyChangedMethodName = onPropertyChangedMethodName;
                OnPropertyChangingMethodName = onPropertyChangingMethodName;
            }
        }

        public static CompleterResult Generate(SemanticModel model, INamedTypeSymbol type) {
            return GenerateCore(model, type);
        }

        static string GenerateCore(SemanticModel model, INamedTypeSymbol type) {
            var bindablePropertyAttributeType = model.Compilation.GetTypeByMetadataName("DevExpress.Mvvm.DataAnnotations.BindablePropertyAttribute");
            var methods = type.Methods()
                .ToImmutableDictionary(x => x.Name, x => x);
            var properties = type.Properties()
                .Select(property => {
                    var bindableInfo = property.GetAttributes()
                        .FirstOrDefault(x => x.AttributeClass == bindablePropertyAttributeType) 
                        .With(x => {
                            var args = x.ConstructorArguments.Select(arg => arg.Value).ToArray();
                            var namedArgs = x.NamedArguments.ToImmutableDictionary(p => p.Key, p => (string)p.Value.Value); //TODO error if names are not recognizable
                            return new BindableInfo(args.Length > 0 ? (bool)args[0] : true, 
                                namedArgs.GetValueOrDefault("OnPropertyChangedMethodName"),
                                namedArgs.GetValueOrDefault("OnPropertyChangingMethodName")); 
                        });
                    return new { property, bindableInfo };
                })
                .Where(x => {
                    return (x.property.IsVirtual && x.bindableInfo.Return(bi => bi.IsBindable, () => true))
                        && x.property.DeclaredAccessibility == Accessibility.Public
                        && x.property.GetMethod.DeclaredAccessibility == Accessibility.Public
                        && (x.property.IsAutoImplemented() || x.bindableInfo.Return(bi => bi.IsBindable, () => false));
                })
                .Select(info => {
                    var property = info.property;
                    var setterModifier = property.SetMethod.DeclaredAccessibility.ToAccessibilityModifier(property.DeclaredAccessibility);

                    //TODO diplicated code
                    var onChangedMethodName = info.bindableInfo?.OnPropertyChangedMethodName ?? $"On{property.Name}Changed".If(x => property.IsAutoImplemented());
                    var onChangedMethod = onChangedMethodName.With(x => methods.GetValueOrDefault(x));
                    var needOldValue = onChangedMethod.Return(x => x.Parameters.Length == 1, () => false);
                    var oldValueStorage = needOldValue ? $"var oldValue = base.{property.Name};".AddTabs(2) : null;
                    var oldValueName = needOldValue ? "oldValue" : null;
                    var onChangedMethodCall = onChangedMethod.With(x => $"{x.Name}({oldValueName});".AddTabs(2));

                    var onChangingMethodName = info.bindableInfo?.OnPropertyChangingMethodName ?? $"On{property.Name}Changing".If(x => property.IsAutoImplemented());
                    var onChangingMethod = onChangingMethodName.With(x => methods.GetValueOrDefault(x));
                    var needNewValue = onChangingMethod.Return(x => x.Parameters.Length == 1, () => false);
                    var newValueName = needNewValue ? "value" : null;
                    var onChangingMethodCall = onChangingMethod.With(x => $"{x.Name}({newValueName});".AddTabs(2));

                    return
$@"public override {property.TypeDisplayString(model)} {property.Name} {{
    get {{ return base.{property.Name}; }}
    {setterModifier}set {{
        if(base.{property.Name} == value)
            return;
{onChangingMethodCall}
{oldValueStorage}
        base.{property.Name} = value;
        RaisePropertyChanged(""{property.Name}"");
{onChangedMethodCall}
    }}
}}";
                })
                .ConcatStringsWithNewLines();
            return
//TODO what if System.ComponentModel is already in context?
$@"using System.ComponentModel;
partial class {type.Name} : INotifyPropertyChanged, DevExpress.Mvvm.ISupportParentViewModel {{
    public static {type.Name} Create() {{
        return new {type.Name}Implementation();
    }}
{Implemetations(type.Name)}
    class {type.Name}Implementation : {type.Name} {{
{properties.AddTabs(2)}
    }}
}}";
        }
    }
}