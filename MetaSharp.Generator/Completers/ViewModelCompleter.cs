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
    //TODO do not generate command for method from base class if there is already one
    //TODO do generate command for method from base class if there no one and this method is accessible from completer

    //TODO generate typed parent viewmode if view model has TParent view model parameter
    //TODO auto calc dependent properties
    //TODO auto generate default private ctor if none, use explicit factory methods directly
    //TODO error if base class ctor is used
    //TODO error if existing ctor not private
    //TODO implement INPC in class, not in inherited class, so you can call RaisePropertyChanged without extension methods
    //TODO INotifyPropertyChanging support

    //TODO error is base class supports INPC, but has no RaisePropertyChanged method
    public static class ViewModelCompleter {
        public static readonly Func<string, string> INPCImplemetation = typeName =>
$@"public event PropertyChangedEventHandler PropertyChanged;
void RaisePropertyChanged(string property) {{
    var handler = PropertyChanged;
    if(handler != null)
        handler(this, new PropertyChangedEventArgs(property));
}}
void RaisePropertyChanged<T>(Expression<Func<{typeName}, T>> property) {{
    RaisePropertyChanged(DevExpress.Mvvm.Native.ExpressionHelper.GetPropertyName(property));
}}";
        public static readonly Func<string, string> ParentViewModelImplementation = typeName =>
$@"object parentViewModel;
object ISupportParentViewModel.ParentViewModel {{
    get {{ return parentViewModel; }}
    set {{
        if(parentViewModel == value)
            return;
        var oldParentViewModel = parentViewModel;
        parentViewModel = value;
        OnParentViewModelChanged(oldParentViewModel);
    }}
}}
partial void OnParentViewModelChanged(object oldParentViewModel);";
        public static readonly Func<string, string> SupportServicesImplementation = typeName =>
$@"IServiceContainer _ServiceContainer;
IServiceContainer ISupportServices.ServiceContainer {{ get {{ return _ServiceContainer ?? (_ServiceContainer = new ServiceContainer(this)); }} }}";

        public static readonly string KnownTypes = //TODO do not add this stub if Mvvm is already referenced via MetaReference?? (can't find how to write test for it)
@"
using System;
using System.ComponentModel;
namespace DevExpress.Mvvm {
    public interface ISupportParentViewModel { }
    public interface ISupportServices { }
    public class BindableBase : INotifyPropertyChanged { }
    public class ViewModelBase : BindableBase, ISupportParentViewModel, ISupportServices { }
}
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
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class CommandAttribute : Attribute {
        public CommandAttribute(bool isCommand) {
            this.IsCommand = isCommand;
        }
        public CommandAttribute()
            : this(true) {
        }
        public string Name { get; set; }
        public string CanExecuteMethodName { get; set; }
        public bool UseCommandManager { get; set; }
    }
    public class AsyncCommandAttribute : CommandAttribute {
        public AsyncCommandAttribute(bool isAsincCommand)
            : base(isAsincCommand) { }
        public AsyncCommandAttribute()
            : base() { }
        public bool AllowMultipleExecution { get; set; }
    }
}
";
        public static readonly ImmutableArray<string> Usings = ImmutableArray.Create(
            "System",
            "System.ComponentModel",
            "System.Linq.Expressions", 
            "System.Windows.Input", 
            "DevExpress.Mvvm",
            "DevExpress.Mvvm.POCO");

        class BindableInfo { //TODO make struct, auto-completed (self-hosting)
            public readonly bool IsBindable;
            public readonly string OnPropertyChangedMethodName, OnPropertyChangingMethodName;
            public BindableInfo(bool isBindable, string onPropertyChangedMethodName, string onPropertyChangingMethodName) {
                IsBindable = isBindable;
                OnPropertyChangedMethodName = onPropertyChangedMethodName;
                OnPropertyChangingMethodName = onPropertyChangingMethodName;
            }
        }
        class AsyncCommandInfo { //TODO make struct, auto-completed (self-hosting)
            public readonly bool IsCommand, AllowMultipleExecution, UseCommandManager;
            public readonly string Name, CanExecuteMethodName;

            public AsyncCommandInfo(bool isCommand, bool allowMultipleExecution, bool useCommandManager, string name, string canExecuteMethodName) {
                IsCommand = isCommand;
                AllowMultipleExecution = allowMultipleExecution;
                UseCommandManager = useCommandManager;
                Name = name;
                CanExecuteMethodName = canExecuteMethodName;
            }
        }

        public static CompleterResult Generate(SemanticModel model, INamedTypeSymbol type) {
            return GenerateCore(model, type);
        }

        static string GenerateCore(SemanticModel model, INamedTypeSymbol type) {
            var commands = GenerateCommands(model, type);
            var properties = GenerateProperties(model, type);

            Func<Func<string, string>, string, string> getImplementation = (getImpl, interfaceName) => {
                var interfaceType = model.Compilation.GetTypeByMetadataName(interfaceName);
                return type.AllInterfaces.Contains(interfaceType)
                    ? string.Empty
                    : getImpl(type.Name).AddTabs(1);
            };
            var inpcImplementation = getImplementation(INPCImplemetation, "System.ComponentModel.INotifyPropertyChanged");
            var parentViewModelImplementation = getImplementation(ParentViewModelImplementation, "DevExpress.Mvvm.ISupportParentViewModel");
            var supportServicesImplementation = getImplementation(SupportServicesImplementation, "DevExpress.Mvvm.ISupportServices");

            return
$@"partial class {type.Name} : INotifyPropertyChanged, ISupportParentViewModel, ISupportServices {{
    public static {type.Name} Create() {{
        return new {type.Name}Implementation();
    }}
{commands.AddTabs(1)}
{inpcImplementation}
{parentViewModelImplementation}
{supportServicesImplementation}
    class {type.Name}Implementation : {type.Name}, IPOCOViewModel {{
{properties.AddTabs(2)}
        void IPOCOViewModel.RaisePropertyChanged(string propertyName) {{
            RaisePropertyChanged(propertyName);
        }}
    }}
}}";
        }

        static string GenerateCommands(SemanticModel model, INamedTypeSymbol type) {
            var asyncCommandAttributeType = model.Compilation.GetTypeByMetadataName("DevExpress.Mvvm.DataAnnotations.AsyncCommandAttribute");
            var commandAttributeType = model.Compilation.GetTypeByMetadataName("DevExpress.Mvvm.DataAnnotations.CommandAttribute");

            var taskType = model.Compilation.GetTypeByMetadataName(typeof(Task).FullName);
            var methodsMap = type.Methods()
                .ToImmutableDictionary(x => x.Name, x => x);
            return type.Methods()
                .Select(method => {
                    var asyncCommandInfo = method.GetAttributes()
                        .FirstOrDefault(x => x.AttributeClass == asyncCommandAttributeType || x.AttributeClass == commandAttributeType)
                        .With(x => {
                            var args = x.ConstructorArguments.Select(arg => arg.Value).ToArray(); //TODO dup code
                            var namedArgs = x.NamedArguments.ToImmutableDictionary(p => p.Key, p => p.Value.Value); //TODO error if names are not recognizable
                            return new AsyncCommandInfo(
                                isCommand: args.Length > 0 ? (bool)args[0] : true,
                                allowMultipleExecution: (bool)namedArgs.GetValueOrDefault("AllowMultipleExecution", false),
                                useCommandManager: (bool)namedArgs.GetValueOrDefault("UseCommandManager", true),
                                name: (string)namedArgs.GetValueOrDefault("Name"), 
                                canExecuteMethodName: (string)namedArgs.GetValueOrDefault("CanExecuteMethodName"));
                        });
                    return new { method, asyncCommandInfo };
                })
                .Where(info => (info.asyncCommandInfo?.IsCommand ?? (info.method.DeclaredAccessibility == Accessibility.Public))
                    && info.method.MethodKind == MethodKind.Ordinary
                    && !info.method.IsStatic
                    && (info.method.ReturnsVoid || info.method.ReturnType == taskType || (info.asyncCommandInfo?.IsCommand ?? false))
                    && (!info.method.Parameters.Any() || (info.method.Parameters.Length == 1  && info.method.Parameters.Single().RefKind == RefKind.None)))
                .Select(info => {
                    var isAsync = info.method.ReturnType == taskType;
                    var commandName = info.asyncCommandInfo?.Name ?? (info.method.Name + "Command");
                    var methodName = info.method.Name;
                    var genericParameter = info.method.Parameters.SingleOrDefault()
                        .With(x => "<" + x.Type.DisplayString(model, x.Location()) + ">");
                    if(!info.method.ReturnsVoid && info.method.ReturnType != taskType) {
                        if(genericParameter == null)
                            methodName = $"() => {methodName}()";
                        else
                            methodName = $"x => {methodName}(x)";
                    }
                    var commandTypeName = (isAsync ? "AsyncCommand" : "DelegateCommand") + genericParameter;
                    var propertyType = isAsync 
                        ? "AsyncCommand" + genericParameter
                        : (genericParameter.With(x => $"DelegateCommand{x}") ?? "ICommand");
                    var canExecuteMethodName = ", " + (info.asyncCommandInfo?.CanExecuteMethodName ?? (methodsMap.GetValueOrDefault("Can" + methodName)?.Name ?? "null"));
                    var allowMultipleExecution = (info.asyncCommandInfo?.AllowMultipleExecution ?? false) ? ", allowMultipleExecution: true" : null;
                    var useCommandManager = !(info.asyncCommandInfo?.UseCommandManager ?? true) ? ", useCommandManager: false" : null;
                    return
$@"{commandTypeName} _{commandName};
public {propertyType} {commandName} {{ get {{ return _{commandName} ?? (_{commandName} = new {commandTypeName}({methodName}{canExecuteMethodName}{allowMultipleExecution}{useCommandManager})); }} }}";
                })
                .ConcatStringsWithNewLines();
        }

        static string GenerateProperties(SemanticModel model, INamedTypeSymbol type) {
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
            return properties;
        }
    }
}