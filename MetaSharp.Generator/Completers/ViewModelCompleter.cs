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
using MetaSharp.Utils;
using CompleterResult = MetaSharp.Either<System.Collections.Immutable.ImmutableArray<MetaSharp.CompleterError>, string>;

namespace MetaSharp {
    //TODO GENERIC TYPES
    //TODO IMPLEMENT INTERFACE USING SEPARATE IMPLEMENTOR CLASS
    //TODO IMPLEMENT ATTRIBUTE PARSER AND ANALIZE ATTRIBUTES USING IT

    //TODO do not generate command for method from base class if there is already one
    //TODO do generate command for method from base class if there no one and this method is accessible from completer

    //TODO auto calc dependent properties
    //TODO auto generate default private ctor if none, 
    //TODO write user-defined factory methods (do not overwrite them or attribute to hide specific ctor)
    //TODO error if base class ctor is used
    //TODO error if existing ctor not private
    //TODO implement INPC in class, not in inherited class, so you can call RaisePropertyChanged without extension methods
    //TODO INotifyPropertyChanging support
    //TODO group output in regions

    //TODO error is base class supports INPC, but has no RaisePropertyChanged method
    //TODO copy attributes to overriden properties and methods (the same which POCO already copies)
    //TODO warnings if class has public ctors

    //TODO ignore POCOViewModel attribute or not??
    //TODO coerce callback for returning values
    //TODO POCO class with errors in more than 1 place (find all .Node() usages here and in all other code)
    //TODO allow property changed method which return value if explicitly specified in BindablePropertyAttribute??

    //TODO .With(x => x()) ==> .WithFunc()
    //TODO determine whether properties and methods from base classes are property overriden
    //TODO generate RaisePropertyChanged method if class implements INotifyPropertyChanged but RaisePropertyChanged doesn't exist (explicit and implicit PropertyChanged implementations)

    //TODO use method matcher (all places where RefType is checked)
    public class ViewModelCompleter {
        #region constants
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

        public static readonly string DataErrorInfoErrorImplementation =
"string IDataErrorInfo.Error { get { return string.Empty; } }";
        public static readonly string DataErrorInfoIndexerImplementation =
"string IDataErrorInfo.this[string columnName] { get { return IDataErrorInfoHelper.GetErrorText(this, columnName); } }"; 

        public static readonly string KnownTypes = //TODO do not add this stub if Mvvm is already referenced via MetaReference?? (can't find how to write test for it)
@"
using System;
using System.ComponentModel;
namespace DevExpress.Mvvm {
    public interface ISupportParentViewModel { }
    public interface ISupportServices { }
    public class BindableBase : INotifyPropertyChanged {
        void RaisePropertyChanged(string propertyName) { }
    }
    public class ViewModelBase : BindableBase, ISupportParentViewModel, ISupportServices { }
}
namespace DevExpress.Mvvm.POCO {
    public interface IPOCOViewModel { 
        void RaisePropertyChanged(string propertyName);
    }
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
        #endregion

        #region inner classes
        class BindableInfo { //TODO make struct, auto-completed (self-hosting)
            public readonly bool IsBindable;
            public readonly string OnPropertyChangedMethodName, OnPropertyChangingMethodName;
            public BindableInfo(bool isBindable, string onPropertyChangedMethodName, string onPropertyChangingMethodName) {
                IsBindable = isBindable;
                OnPropertyChangedMethodName = onPropertyChangedMethodName;
                OnPropertyChangingMethodName = onPropertyChangingMethodName;
            }
        }
        class CommandInfo { //TODO make struct, auto-completed (self-hosting)
            public readonly bool IsCommand, AllowMultipleExecution, UseCommandManager;
            public readonly string Name, CanExecuteMethodName;

            public CommandInfo(bool isCommand, bool allowMultipleExecution, bool useCommandManager, string name, string canExecuteMethodName) {
                IsCommand = isCommand;
                AllowMultipleExecution = allowMultipleExecution;
                UseCommandManager = useCommandManager;
                Name = name;
                CanExecuteMethodName = canExecuteMethodName;
            }
        }
        #endregion

        public static CompleterResult Generate(SemanticModel model, INamedTypeSymbol type) {
            return new ViewModelCompleter(model, type).GenerateCore();
        }

        readonly SemanticModel model; 
        readonly INamedTypeSymbol type, bindablePropertyAttributeType, taskType;
        readonly ImmutableDictionary<string, ImmutableArray<IMethodSymbol>> methods;

        ViewModelCompleter(SemanticModel model, INamedTypeSymbol type) {
            this.model = model;
            this.type = type;
            bindablePropertyAttributeType = model.Compilation.GetTypeByMetadataName("DevExpress.Mvvm.DataAnnotations.BindablePropertyAttribute");
            methods = type.AllMethods()
                .ToLookup(x => x.Name)
                .ToImmutableDictionary(x => x.Key, x => x.ToImmutableArray());
            taskType = model.Compilation.GetTypeByMetadataName(typeof(Task).FullName);
        }

        CompleterResult GenerateCore() {
            var classErrors = GetClassErrors().ToImmutableArray();
            if(classErrors.Any())
                return classErrors;

            var res = Either.Combine(
                CompleterResult.Right(GenerateCommands()),
                GenerateProperties(), 
                Either<ImmutableArray<CompleterError>, Tuple<string, string>>.Right(GenerateCreateMethodsAndConstructors()),
                GetImplementations(),
                (commands, properties, createMethodsAndConstructors, implementations) => {
                    return 
        $@"partial class {type.Name} : INotifyPropertyChanged, ISupportParentViewModel, ISupportServices {{
{createMethodsAndConstructors.Item1.AddTabs(1)}
{commands.AddTabs(1)}
{implementations}
    class {type.Name}Implementation : {type.Name}, IPOCOViewModel {{
{createMethodsAndConstructors.Item2.AddTabs(2)}
{properties.AddTabs(2)}
        void IPOCOViewModel.RaisePropertyChanged(string propertyName) {{
            RaisePropertyChanged(propertyName);
        }}
    }}
}}";
                })
                .SelectError(erorrs => erorrs.SelectMany(x => x).ToImmutableArray());
            return res;

        }
        IEnumerable<CompleterError> GetClassErrors() {
            if(type.IsSealed)
                yield return CompleterError.CreateForTypeName(type, Messages.POCO_SealedClass.Format(type.Name));
            var iPOCOViewModeType = model.Compilation.GetTypeByMetadataName("DevExpress.Mvvm.POCO.IPOCOViewModel");
            if(type.AllInterfaces.Contains(iPOCOViewModeType))
                yield return CompleterError.CreateForTypeName(type, Messages.POCO_TypeImplementsIPOCOViewModel.Format(type.Name));
        }

        CompleterResult GetImplementations() {
            Func<Func<string, string>, string, Func<CompleterError>, Either<CompleterError, string>> getImplementation = (getImpl, interfaceName, check) => {
                var interfaceType = model.Compilation.GetTypeByMetadataName(interfaceName);
                if(type.AllInterfaces.Contains(interfaceType)) {
                    var error = check.With(x => x());
                    if(error != null)
                        return error;
                    return string.Empty;
                }
                return getImpl(type.Name).AddTabs(1);
            };
            const string IDataErrorInfoName = "System.ComponentModel.IDataErrorInfo";
            Func<string, string, string> getIDataErrorInfoPropertyImplementation = (implementation, name) =>
                 type.Properties().Any(m => (m.Name == IDataErrorInfoName + "." + name) || (m.DeclaredAccessibility == Accessibility.Public && m.Name == name))
                     ? null
                     : implementation;
            var iDataErrorInfoType = model.Compilation.GetTypeByMetadataName(IDataErrorInfoName);
            var dataErrorInfoImplementation = type.AllInterfaces.Contains(iDataErrorInfoType)
                ? (
                    getIDataErrorInfoPropertyImplementation(DataErrorInfoErrorImplementation, "Error")
                    +
                    getIDataErrorInfoPropertyImplementation(DataErrorInfoIndexerImplementation, "this[]")
                ) : null;
            Func<CompleterError> checkRaisePropertyChangedMethod = () => {
                Func<CompleterError> error = () => CompleterError.CreateForTypeName(type, Messages.POCO_RaisePropertyChangedMethodNotFound.Format(type.Name));
                var raisePropertyChangedMethods = methods.GetValueOrDefault("RaisePropertyChanged", ImmutableArray<IMethodSymbol>.Empty);
                if(raisePropertyChangedMethods.IsEmpty)
                    return error();
                var raisePropertyChangedMethod = raisePropertyChangedMethods.FirstOrDefault(method => {
                    if(method.Parameters.Length != 1)
                        return false;
                    var parameter = method.Parameters.Single();
                    return parameter.RefKind == RefKind.None && parameter.Type.SpecialType == SpecialType.System_String;
                });
                if(raisePropertyChangedMethod == null)
                    return error();
                return null;
            };
            return new[] {
                getImplementation(INPCImplemetation, "System.ComponentModel.INotifyPropertyChanged", checkRaisePropertyChangedMethod),
                getImplementation(ParentViewModelImplementation, "DevExpress.Mvvm.ISupportParentViewModel", null),
                getImplementation(SupportServicesImplementation, "DevExpress.Mvvm.ISupportServices", null),
                dataErrorInfoImplementation,
            }
            .AggregateEither(errors => errors.ToImmutableArray(), values => values.ConcatStringsWithNewLines());
        }

        Tuple<string, string> GenerateCreateMethodsAndConstructors() {
            var paramsAndArgs = type.Constructors()
                .Select(method => {
                    var parameters = method.IsImplicitlyDeclared 
                        ? SyntaxFactory.ParameterList().Parameters 
                        : ((ConstructorDeclarationSyntax)method.Node()).ParameterList.Parameters;
                    var arguments = parameters.Select(x => x.Identifier.ToString()).ConcatStrings(", ");
                    return new { parameters, arguments };
                })
                .ToImmutableArray();
            var createMethods = paramsAndArgs
                .Select(info => {
                    return
$@"public static {type.Name} Create({info.parameters}) {{
    return new {type.Name}Implementation({info.arguments});
}}";
                })
                .ConcatStringsWithNewLines();
            var constructors = paramsAndArgs
                .Select(info => {
                    return
$@"public {type.Name}Implementation({info.parameters}) 
    :base({info.arguments}) {{ }}";
                })
                .ConcatStringsWithNewLines();
            return Tuple.Create(createMethods, constructors);
        }

        ImmutableArray<IMethodSymbol> GetMethods(string name) {
            return methods.GetValueOrDefault(name, ImmutableArray<IMethodSymbol>.Empty);
        }

        #region commands
        string GenerateCommands() {
            var asyncCommandAttributeType = model.Compilation.GetTypeByMetadataName("DevExpress.Mvvm.DataAnnotations.AsyncCommandAttribute");
            var commandAttributeType = model.Compilation.GetTypeByMetadataName("DevExpress.Mvvm.DataAnnotations.CommandAttribute");

            return type.Methods()
                .Select(method => {
                    var commandInfo = method.GetAttributes()
                        .FirstOrDefault(x => x.AttributeClass == asyncCommandAttributeType || x.AttributeClass == commandAttributeType)
                        .With(x => {
                            var args = x.ConstructorArguments.Select(arg => arg.Value).ToArray(); //TODO dup code
                            var namedArgs = x.NamedArguments.ToImmutableDictionary(p => p.Key, p => p.Value.Value); //TODO error if names are not recognizable
                            return new CommandInfo(
                                isCommand: args.Length > 0 ? (bool)args[0] : true,
                                allowMultipleExecution: (bool)namedArgs.GetValueOrDefault("AllowMultipleExecution", false),
                                useCommandManager: (bool)namedArgs.GetValueOrDefault("UseCommandManager", true),
                                name: (string)namedArgs.GetValueOrDefault("Name"),
                                canExecuteMethodName: (string)namedArgs.GetValueOrDefault("CanExecuteMethodName"));
                        });
                    return new { method, commandInfo };
                })
                .Where(info => (info.commandInfo?.IsCommand ?? (info.method.DeclaredAccessibility == Accessibility.Public))
                    && info.method.MethodKind == MethodKind.Ordinary
                    && !info.method.IsStatic
                    && (info.method.ReturnsVoid || info.method.ReturnType == taskType || (info.commandInfo?.IsCommand ?? false))
                    && (!info.method.Parameters.Any() || (info.method.Parameters.Length == 1 && info.method.Parameters.Single().RefKind == RefKind.None)))
                .Select(info => {
                    return GenerateCommand(info.method, info.commandInfo);
                })
                .ConcatStringsWithNewLines();
        }
        string GenerateCommand(IMethodSymbol method, CommandInfo commandInfo) {
            var isAsync = method.ReturnType == taskType;
            var commandName = commandInfo?.Name ?? (method.Name + "Command");
            var methodName = method.Name;
            var genericParameter = method.Parameters.SingleOrDefault()
                .With(x => "<" + x.Type.DisplayString(model, x.Location()) + ">");
            if(!method.ReturnsVoid && method.ReturnType != taskType) {
                if(genericParameter == null)
                    methodName = $"() => {methodName}()";
                else
                    methodName = $"x => {methodName}(x)";
            }
            var commandTypeName = (isAsync ? "AsyncCommand" : "DelegateCommand") + genericParameter;
            var propertyType = isAsync
                ? "AsyncCommand" + genericParameter
                : (genericParameter.With(x => $"DelegateCommand{x}") ?? "ICommand");
            var canExecuteMethodName = ", " + (commandInfo?.CanExecuteMethodName ?? (GetMethods("Can" + methodName).SingleOrDefault()?.Name ?? "null"));
            var allowMultipleExecution = (commandInfo?.AllowMultipleExecution ?? false) ? ", allowMultipleExecution: true" : null;
            var useCommandManager = !(commandInfo?.UseCommandManager ?? true) ? ", useCommandManager: false" : null;
            return
$@"{commandTypeName} _{commandName};
public {propertyType} {commandName} {{ get {{ return _{commandName} ?? (_{commandName} = new {commandTypeName}({methodName}{canExecuteMethodName}{allowMultipleExecution}{useCommandManager})); }} }}";
        }
        #endregion


        #region properties
        CompleterResult GenerateProperties() {
            var properties = type.Properties()
                .Select(property => {
                    var bindableInfo = GetBindableInfo(property);
                    return new { property, bindableInfo };
                })
                .Select(info => IsBindable(info.property, info.bindableInfo)
                    .Select(isBindableValue => isBindableValue ? info : null)
                 )
                .WhereEither(x => x != null)
                .Select(x => x.SelectMany(info => GenerateProperty(info.property, info.bindableInfo)))
                .AggregateEither(errors => errors.ToImmutableArray(), values => values.ConcatStringsWithNewLines());
            return properties;
        }
        BindableInfo GetBindableInfo(IPropertySymbol property) { 
            return property.GetAttributes()
                .FirstOrDefault(x => x.AttributeClass == bindablePropertyAttributeType)
                .With(x => {
                    var args = x.ConstructorArguments.Select(arg => arg.Value).ToArray();
                    var namedArgs = x.NamedArguments.ToImmutableDictionary(p => p.Key, p => (string)p.Value.Value); //TODO error if names are not recognizable
                    return new BindableInfo(args.Length > 0 ? (bool)args[0] : true,
                        namedArgs.GetValueOrDefault("OnPropertyChangedMethodName"),
                        namedArgs.GetValueOrDefault("OnPropertyChangingMethodName"));
                });
        }
        Either<CompleterError, bool> IsBindable(IPropertySymbol property, BindableInfo bindableInfo) {
            if(bindableInfo?.IsBindable ?? false) {
                if(property.IsSealed)
                    return CompleterError.CreatePropertyError(property, Messages.POCO_PropertyIsSealed);
                if(!property.IsVirtual)
                    return CompleterError.CreatePropertyError(property, Messages.POCO_PropertyIsNotVirual);
                if(property.IsReadOnly)
                    return CompleterError.CreatePropertyError(property, Messages.POCO_PropertyHasNoSetter);
                if(property.GetMethod.DeclaredAccessibility != Accessibility.Public)
                    return CompleterError.CreatePropertyError(property, Messages.POCO_PropertyHasNoPublicGetter);
            }
            return property.IsVirtual
                && (bindableInfo?.IsBindable ?? true)
                && property.DeclaredAccessibility == Accessibility.Public
                && property.GetMethod.DeclaredAccessibility == Accessibility.Public
                && property.IsAutoImplemented() || bindableInfo.Return(bi => bi.IsBindable, () => false);
        }
        struct MethodCallInfo {
            public readonly string MethodCall;
            public readonly bool NeedParameter;

            public MethodCallInfo(string methodCall, bool needParameter) {
                MethodCall = methodCall;
                NeedParameter = needParameter;
            }
        }
        Either<CompleterError, string> GenerateProperty(IPropertySymbol property, BindableInfo bindableInfo) {

            Func<Chang, string, Either<CompleterError, MethodCallInfo>> getCallInfo = (chang, attributeMethodName) => {
                var methodName = attributeMethodName ?? $"On{property.Name}Chang{chang}".If(x => property.IsAutoImplemented());
                if(methodName != null && GetMethods(methodName).Length > 1)
                    return CompleterError.CreatePropertyError(property, Messages.POCO_MoreThanOnePropertyChangedMethod(chang));
                var method = methodName.With(x => GetMethods(x).SingleOrDefault());
                if(method == null && attributeMethodName != null)
                    return CompleterError.CreateForPropertyName(property, Messages.POCO_PropertyChangedMethodNotFound(chang).Format(attributeMethodName));
                if(method != null) {
                    if(method.Parameters.Length > 1)
                        return CompleterError.CreateMethodError(method, Messages.POCO_PropertyChangedCantHaveMoreThanOneParameter(chang));
                    if(!method.ReturnsVoid)
                        return CompleterError.CreateMethodError(method, Messages.POCO_PropertyChangedCantHaveReturnType(chang));
                    if(method.Parameters.Length == 1 && method.Parameters.Single().Type != property.Type)
                        return CompleterError.CreateParameterError(method.Parameters.Single(), Messages.POCO_PropertyChangedMethodArgumentTypeShouldMatchPropertyType(chang));
                }
                var needParameter = method.Return(x => x.Parameters.Length == 1, () => false);
                var valueName = needParameter ? (chang == Chang.ed ? "oldValue" : "value") : null;
                var methodCall = method.With(x => $"{x.Name}({valueName});".AddTabs(2));
                return new MethodCallInfo(methodCall, needParameter);
            };

            return from changed in getCallInfo(Chang.ed, bindableInfo?.OnPropertyChangedMethodName)
                   from changing in getCallInfo(Chang.ing, bindableInfo?.OnPropertyChangingMethodName)
                   select GenerateProperty(property, changed, changing);
        }

        string GenerateProperty(IPropertySymbol property, MethodCallInfo changed, MethodCallInfo changing) {
            var setterModifier = property.SetMethod.DeclaredAccessibility.ToAccessibilityModifier(property.DeclaredAccessibility);
            var oldValueStorage = changed.NeedParameter ? $"var oldValue = base.{property.Name};".AddTabs(2) : null;
            return
$@"public override {property.TypeDisplayString(model)} {property.Name} {{
    get {{ return base.{property.Name}; }}
    {setterModifier}set {{
        if(base.{property.Name} == value)
            return;
{changing.MethodCall}
{oldValueStorage}
        base.{property.Name} = value;
        RaisePropertyChanged(""{property.Name}"");
{changed.MethodCall}
    }}
}}";
        }
        #endregion
    }
}