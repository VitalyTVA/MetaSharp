using MetaSharp.Native;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace MetaSharp.Test {
    public class CompleterTests : GeneratorTestsBase {
        #region class
        [Fact]
        public void CompletePrototypeFiles() {
            var input = GetInput(new[] { @"IncompleteClasses1.cs", @"IncompleteClasses2.cs" }) + 
@"
namespace MetaSharp.HelloWorld {
    public partial class NoCompletion {
        public int IntProperty { get; }
    }
}
";
            string incomplete1 =
@"
using MetaSharp;
namespace MetaSharp.Incomplete {
using FooBoo;
    [MetaCompleteClass]
    public partial class Foo {
        public Boo BooProperty { get; }
        public FooBoo.Moo MooProperty { get; }
        public int IntProperty { get; }
    }
    [MetaCompleteClass]
    public partial class Foo2 {
        public Boo BooProperty { get; }
    }
    public partial class NoCompletion {
        public int IntProperty { get; }
    }
}";
            string incomplete2 =
@"
using MetaSharp;
namespace MetaSharp.Incomplete {
using FooBoo;
    [MetaCompleteClass]
    public partial class Foo3 {
        public Boo BooProperty { get; }
    }
}";

            string output1 =
@"namespace MetaSharp.Incomplete {
using FooBoo;
    partial class Foo {
        public Foo(Boo booProperty, FooBoo.Moo mooProperty, int intProperty) {
            BooProperty = booProperty;
            MooProperty = mooProperty;
            IntProperty = intProperty;
        }
    }
}
namespace MetaSharp.Incomplete {
using FooBoo;
    partial class Foo2 {
        public Foo2(Boo booProperty) {
            BooProperty = booProperty;
        }
    }
}";
            string output2 =
@"namespace MetaSharp.Incomplete {
using FooBoo;
    partial class Foo3 {
        public Foo3(Boo booProperty) {
            BooProperty = booProperty;
        }
    }
}";
            string additionalClasses = @"
namespace FooBoo {
    public class Boo {
        public string BooProp { get; set; }
    }
    public class Moo { 
        public int MooProp { get; set; }
    }
}";
            var name1 = "IncompleteClasses1.cs";
            var name2 = "IncompleteClasses2.cs";
            AssertMultipleFilesOutput(
                ImmutableArray.Create(
                    new TestFile(SingleInputFileName, input),
                    new TestFile(name1, incomplete1, isInFlow: false),
                    new TestFile(name2, incomplete2, isInFlow: false)
                ),
                ImmutableArray.Create(
                    new TestFile(Path.Combine(DefaultIntermediateOutputPath, "IncompleteClasses1.g.i.cs"), output1),
                    new TestFile(Path.Combine(DefaultIntermediateOutputPath, "IncompleteClasses2.g.i.cs"), output2)
                ),
                ignoreEmptyLines: true
            );
            AssertCompiles(new[] { input, incomplete1, incomplete2, output1, output2, additionalClasses });
        }
        [Fact]
        public void CompletePrototypeFiles_TypeNameWithNameSpace_ShortName_Alias() {
            string incomplete =
@"
using MetaSharp;
namespace MetaSharp.Incomplete {
using System;
using Qoo = FooBoo.Moo;
    [MetaCompleteClass]
    public partial class Foo {
        public FooBoo.Boo BooProperty { get; }
        public Qoo QooProperty { get; }
        public FooBoo.Moo MooProperty { get; }
        public Int32 IntProperty { get; }
    }
}";

            string output =
@"namespace MetaSharp.Incomplete {
using System;
using Qoo = FooBoo.Moo;
    partial class Foo {
        public Foo(FooBoo.Boo booProperty, Qoo qooProperty, Qoo mooProperty, int intProperty) {
            BooProperty = booProperty;
            QooProperty = qooProperty;
            MooProperty = mooProperty;
            IntProperty = intProperty;
        }
    }
}";
            string additionalClasses = @"
namespace FooBoo {
    public class Boo {
    }
    public class Moo {
    }
}";
            var name = "IncompleteClasses.cs";
            var input = GetInput(@"IncompleteClasses.cs".Yield(), "[MetaLocation(MetaLocation.Project)]");
            AssertMultipleFilesOutput(
                ImmutableArray.Create(
                    new TestFile(SingleInputFileName, input),
                    new TestFile(name, incomplete, isInFlow: false)
                ),
                ImmutableArray.Create(
                    new TestFile("IncompleteClasses.designer.cs", output, isInFlow: false)
                ),
                ignoreEmptyLines: true
            );
            AssertCompiles(new[] { input, incomplete, output, additionalClasses });
        }
        [Fact]
        public void CompletePrototypeFiles_DefaultAttributes() {
            var input = GetInput("Incomplete.cs".Yield(), defaultAttributes: ", new[] { new MetaCompleteClassAttribute() }");
            string incomplete =
@"
namespace MetaSharp.Incomplete {
    public partial class Foo {
        public int IntProperty { get; }
    }
    [MetaSharp.MetaCompleteClass]
    public partial class Moo {
        public int IntProperty { get; }
    }
}";

            string output =
@"namespace MetaSharp.Incomplete {
    partial class Foo {
        public Foo(int intProperty) {
            IntProperty = intProperty;
        }
    }
}
namespace MetaSharp.Incomplete {
    partial class Moo {
        public Moo(int intProperty) {
            IntProperty = intProperty;
        }
    }
}";
            var name = "Incomplete.cs";
            AssertMultipleFilesOutput(
                ImmutableArray.Create(
                    new TestFile(SingleInputFileName, input),
                    new TestFile(name, incomplete, isInFlow: false)
                ),
                ImmutableArray.Create(
                    new TestFile(Path.Combine(DefaultIntermediateOutputPath, "Incomplete.g.i.cs"), output)
                ),
                ignoreEmptyLines: true
            );
            AssertCompiles(new[] { input, incomplete, output });
        }
        [Fact]
        public void CompletePrototypeFiles_MultipleAttributes() {
            var input = GetInput(new[] { @"IncompleteClasses.cs" });
            string incomplete =
@"
using MetaSharp;
namespace MetaSharp.Incomplete {
    [MetaCompleteClass, MetaCompleteDependencyProperties]
    public partial class Foo {
        static Foo() {
            DependencyPropertyRegistrator<Foo>.New()
                .Register<string>(x => x. Prop1, out Prop1Property, null)
            ;
        }
        public int IntProperty { get; }
    }
}";
            string output =
@"namespace MetaSharp.Incomplete {
    partial class Foo {
        public Foo(int intProperty) {
            IntProperty = intProperty;
        }
    }
}
namespace MetaSharp.Incomplete {
    partial class Foo {
        public static readonly DependencyProperty Prop1Property;
        public string Prop1 {
            get { return (string)GetValue(Prop1Property); }
            set { SetValue(Prop1Property, value); }
        }
    }
}";
            var name = "IncompleteClasses.cs";
            AssertMultipleFilesOutput(
                ImmutableArray.Create(
                    new TestFile(SingleInputFileName, input),
                    new TestFile(name, incomplete, isInFlow: false)
                ),
                ImmutableArray.Create(
                    new TestFile(Path.Combine(DefaultIntermediateOutputPath, "IncompleteClasses.g.i.cs"), output)
                ),
                ignoreEmptyLines: true
            );
        }
        [Fact]
        public void CompletePrototypeFiles_MultipleDefaultAttributes() {
            var input = GetInput(new[] { @"IncompleteClasses.cs" }, defaultAttributes: ", new Attribute[] { new MetaCompleteClassAttribute(), new MetaCompleteDependencyPropertiesAttribute() }");
            string incomplete =
@"
namespace MetaSharp.Incomplete {
    public partial class Foo {
        static Foo() {
            DependencyPropertyRegistrator<Foo>.New()
                .Register<string>(x => x. Prop1, out Prop1Property, null)
            ;
        }
        public int IntProperty { get; }
    }
}";
            string output =
@"namespace MetaSharp.Incomplete {
    partial class Foo {
        public Foo(int intProperty) {
            IntProperty = intProperty;
        }
    }
}
namespace MetaSharp.Incomplete {
    partial class Foo {
        public static readonly DependencyProperty Prop1Property;
        public string Prop1 {
            get { return (string)GetValue(Prop1Property); }
            set { SetValue(Prop1Property, value); }
        }
    }
}";
            var name = "IncompleteClasses.cs";
            AssertMultipleFilesOutput(
                ImmutableArray.Create(
                    new TestFile(SingleInputFileName, input),
                    new TestFile(name, incomplete, isInFlow: false)
                ),
                ImmutableArray.Create(
                    new TestFile(Path.Combine(DefaultIntermediateOutputPath, "IncompleteClasses.g.i.cs"), output)
                ),
                ignoreEmptyLines: true
            );
        }
        #endregion

        #region view model
        static string GetPOCOImplementations(string typeName) {
            return (ViewModelCompleter.INPCImplemetation(typeName) 
                + "\r\n" 
                + ViewModelCompleter.ParentViewModelImplementation(typeName)
                + "\r\n"
                + ViewModelCompleter.SupportServicesImplementation(typeName))
                .AddTabs(2);
        }

        [Fact]
        public void CompleteViewModel() {
            string incomplete =
@"
using MetaSharp;
namespace MetaSharp.Incomplete {
    [MetaCompleteViewModel]
    public partial class ViewModel {
        public ViewModel() { }
        public ViewModel(int x, Boo boo = default(Boo), string s = ""x"") { }
        public virtual Boo BooProperty { get; set; }
        public virtual int IntProperty { get; set; }
        public void Do(string x) { }
    }
}";

            string output =
$@"namespace MetaSharp.Incomplete {{
using System;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Windows.Input;
using DevExpress.Mvvm;
using DevExpress.Mvvm.POCO;
    partial class ViewModel : INotifyPropertyChanged, ISupportParentViewModel, ISupportServices {{
        public static ViewModel Create() {{
            return new ViewModelImplementation();
        }}
        public static ViewModel Create(int x, Boo boo = default(Boo), string s = ""x"") {{
            return new ViewModelImplementation(x, boo, s);
        }}
        DelegateCommand<string> _DoCommand;
        public DelegateCommand<string> DoCommand {{ get {{ return _DoCommand ?? (_DoCommand = new DelegateCommand<string>(Do, null)); }} }}
{GetPOCOImplementations("ViewModel")}
        class ViewModelImplementation : ViewModel, IPOCOViewModel {{
            public ViewModelImplementation() 
                :base() {{ }}
            public ViewModelImplementation(int x, Boo boo = default(Boo), string s = ""x"") 
                :base(x, boo, s) {{ }}
            public override Boo BooProperty {{
                get {{ return base.BooProperty; }}
                set {{
                    if(base.BooProperty == value)
                        return;
                    base.BooProperty = value;
                    RaisePropertyChanged(""BooProperty"");
                }}
            }}
            public override int IntProperty {{
                get {{ return base.IntProperty; }}
                set {{
                    if(base.IntProperty == value)
                        return;
                    base.IntProperty = value;
                    RaisePropertyChanged(""IntProperty"");
                }}
            }}
            void IPOCOViewModel.RaisePropertyChanged(string propertyName) {{
                RaisePropertyChanged(propertyName);
            }}
        }}
    }}
}}";
            string additionalClasses = @"
namespace MetaSharp.Incomplete {
    public class Boo {
    }
}";
            var name = "IncompleteViewModels.cs";
            var input = GetInput(@"IncompleteViewModels.cs".Yield());
            AssertMultipleFilesOutput(
                ImmutableArray.Create(
                    new TestFile(SingleInputFileName, input),
                    new TestFile(name, incomplete, isInFlow: false)
                ),
                ImmutableArray.Create(
                    new TestFile(Path.Combine(DefaultIntermediateOutputPath, "IncompleteViewModels.g.i.cs"), output)
                ),
                ignoreEmptyLines: true
            );
            AssertCompiles(new[] { input, incomplete, output, additionalClasses }, MvvmDllPath.Yield());
        }

        [Fact]
        public void CompleteViewModel_ExplicitMvvmMetaReference() {
            string incomplete =
@"
using MetaSharp;
using DevExpress.Mvvm.DataAnnotations;
namespace MetaSharp.Incomplete {
using System;
    [MetaCompleteViewModel]
    public partial class ViewModel {
        [BindableProperty(false)]
        public virtual int BooProperty { get; set; }
    }
}";

            string output =
$@"namespace MetaSharp.Incomplete {{
using System;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Windows.Input;
using DevExpress.Mvvm;
using DevExpress.Mvvm.POCO;
    partial class ViewModel : INotifyPropertyChanged, ISupportParentViewModel, ISupportServices {{
        public static ViewModel Create() {{
            return new ViewModelImplementation();
        }}
{GetPOCOImplementations("ViewModel")}
        class ViewModelImplementation : ViewModel, IPOCOViewModel {{
            public ViewModelImplementation() 
                :base() {{ }}
            void IPOCOViewModel.RaisePropertyChanged(string propertyName) {{
                RaisePropertyChanged(propertyName);
            }}
        }}
    }}
}}";
            var name = "IncompleteViewModels.cs";
            var mvvmDirName = Directory.GetDirectories(@"..\..\packages\", "DevExpressMvvm.*").Single();
            var input = GetInput(@"IncompleteViewModels.cs".Yield(), 
                assemblyAttributes: $@"[assembly: MetaSharp.MetaReference(@""{MvvmDllPath}"")]");
            AssertMultipleFilesOutput(
                ImmutableArray.Create(
                    new TestFile(SingleInputFileName, input),
                    new TestFile(name, incomplete, isInFlow: false)
                ),
                ImmutableArray.Create(
                    new TestFile(Path.Combine(DefaultIntermediateOutputPath, "IncompleteViewModels.g.i.cs"), output)
                ),
                ignoreEmptyLines: true
            );
        }
        readonly static string MvvmDllPath = Directory.GetDirectories(@"..\..\packages\", "DevExpressMvvm.*").Single() + @"\lib\net40-client\DevExpress.Mvvm.dll";

        [Fact]
        public void CompleteViewModel_Errors() {
            string incomplete1 =
@"
using MetaSharp;
using DevExpress.Mvvm.DataAnnotations;
namespace MetaSharp.Incomplete {
    public class POCOViewModel_Errors {
        [BindableProperty]
        public string NotVirtualProperty { get; set; }
        [BindableProperty]
        public virtual string NoSetterProperty { get { return null; } }
        [BindableProperty]
        public virtual string PrivateGetterProperty { private get; set; }

        public virtual string MultiplePropertyChanged { get; set; }
        protected void OnMultiplePropertyChangedChanged() { }
        protected void OnMultiplePropertyChangedChanged(string oldValue) { }

        public virtual string TwoParametersChanged { get; set; }
        protected void OnTwoParametersChangedChanged(string a, string b) { }

        public virtual string FuncChanged { get; set; }
        protected int OnFuncChangedChanged() { return 0; }

        [BindableProperty(OnPropertyChangedMethodName = ""MyOnInvalidChangedMethodParameterTypePropertyChanged"")]
        public virtual int InvalidChangedMethodParameterTypeProperty { get; set; }
        protected void MyOnInvalidChangedMethodParameterTypePropertyChanged(double oldValue) { }

        public virtual string MultiplePropertyChanging { get; set; }
        protected void OnMultiplePropertyChangingChanging() { }
        protected void OnMultiplePropertyChangingChanging(string oldValue) { }

        public virtual string TwoParametersChanging { get; set; }
        protected void OnTwoParametersChangingChanging(string a, string b) { }

        public virtual string FuncChanging { get; set; }
        protected int OnFuncChangingChanging() { return 0; }

        [BindableProperty(OnPropertyChangingMethodName = ""MyOnInvalidChangingMethodParameterTypePropertyChanging"")]
        public virtual int InvalidChangingMethodParameterTypeProperty { get; set; }
        protected void MyOnInvalidChangingMethodParameterTypePropertyChanging(double oldValue) { }
    }
}";
            string incomplete2 =
@"
using MetaSharp;
using System.ComponentModel;
using DevExpress.Mvvm.DataAnnotations;
namespace MetaSharp.Incomplete {
    public sealed class POCOViewModel_ClassErrors {
        [BindableProperty]
        public string NotVirtualProperty { get; set; }
    }
    partial class NoRaisePropertyChangedMethod : INotifyPropertyChanged {
        public event PropertyChangedEventHandler PropertyChanged;
    }
    partial class ByRefRaisePopertyChanged : INotifyPropertyChanged {
        public event PropertyChangedEventHandler PropertyChanged;
        void RaisePropertyChanged(ref string x) { }
    }
    partial class OutRaisePopertyChanged : INotifyPropertyChanged {
        public event PropertyChangedEventHandler PropertyChanged;
        void RaisePropertyChanged(out string x) { }
    }
    partial class NoArgsPopertyChanged : INotifyPropertyChanged {
        public event PropertyChangedEventHandler PropertyChanged;
        void RaisePropertyChanged() { }
    }
    partial class WrongParamTypePopertyChanged : INotifyPropertyChanged {
        public event PropertyChangedEventHandler PropertyChanged;
        void RaisePropertyChanged(int propertyName) { }
    }
}";

            var name1 = "IncompleteViewModels1.cs";
            var name2 = "IncompleteViewModels2.cs";
            var input = GetInput(new[] { name1, name2 }, defaultAttributes: ", new[] { new MetaCompleteViewModelAttribute() }");
            AssertMultipleFilesErrors(
                ImmutableArray.Create(
                    new TestFile(SingleInputFileName, input),
                    new TestFile(name1, incomplete1, isInFlow: false),
                    new TestFile(name2, incomplete2, isInFlow: false)
                ),
                errors => Assert.Collection(errors,
                        error => AssertError(error, Path.GetFullPath(name1), Messages.POCO_PropertyIsNotVirual.FullId,
                            "Cannot make non-virtual property bindable: NotVirtualProperty.", 7, 23, 7, 41),
                        error => AssertError(error, Path.GetFullPath(name1), Messages.POCO_PropertyHasNoSetter.FullId,
                            "Cannot make property without setter bindable: NoSetterProperty.", 9, 31, 9, 47),
                        error => AssertError(error, Path.GetFullPath(name1), Messages.POCO_PropertyHasNoPublicGetter.FullId,
                            "Cannot make property without public getter bindable: PrivateGetterProperty.", 11, 31, 11, 52),
                        error => AssertError(error, Path.GetFullPath(name1), Messages.POCO_MoreThanOnePropertyChangedMethod(default(Chang)).FullId,
                            "More than one property changed method: MultiplePropertyChanged.", 13, 31, 13, 54),
                        error => AssertError(error, Path.GetFullPath(name1), Messages.POCO_PropertyChangedCantHaveMoreThanOneParameter(default(Chang)).FullId,
                            "Property changed method cannot have more than one parameter: OnTwoParametersChangedChanged.", 18, 24, 18, 53),
                        error => AssertError(error, Path.GetFullPath(name1), Messages.POCO_PropertyChangedCantHaveReturnType(default(Chang)).FullId,
                            "Property changed method cannot have return type: OnFuncChangedChanged.", 21, 23, 21, 43),
                        error => AssertError(error, Path.GetFullPath(name1), Messages.POCO_PropertyChangedMethodArgumentTypeShouldMatchPropertyType(default(Chang)).FullId,
                            "Property changed method argument type should match property type: MyOnInvalidChangedMethodParameterTypePropertyChanged.", 25, 84, 25, 92),
                        error => AssertError(error, Path.GetFullPath(name1), Messages.POCO_MoreThanOnePropertyChangedMethod(default(Chang)).FullId,
                            "More than one property changing method: MultiplePropertyChanging.", 27, 31, 27, 55),
                        error => AssertError(error, Path.GetFullPath(name1), Messages.POCO_PropertyChangedCantHaveMoreThanOneParameter(default(Chang)).FullId,
                            "Property changing method cannot have more than one parameter: OnTwoParametersChangingChanging.", 32, 24, 32, 55),
                        error => AssertError(error, Path.GetFullPath(name1), Messages.POCO_PropertyChangedCantHaveReturnType(default(Chang)).FullId,
                            "Property changing method cannot have return type: OnFuncChangingChanging.", 35, 23, 35, 45),
                        error => AssertError(error, Path.GetFullPath(name1), Messages.POCO_PropertyChangedMethodArgumentTypeShouldMatchPropertyType(default(Chang)).FullId,
                            "Property changing method argument type should match property type: MyOnInvalidChangingMethodParameterTypePropertyChanging.", 39, 86, 39, 94),

                        error => AssertError(error, Path.GetFullPath(name2), Messages.POCO_SealedClass.FullId,
                            "Cannot create POCO implementation class for the sealed class: POCOViewModel_ClassErrors.", 6, 25, 6, 50),
                        error => AssertError(error, Path.GetFullPath(name2), Messages.POCO_RaisePropertyChangedMethodNotFound.FullId,
                            "Class already supports INotifyPropertyChanged, but RaisePropertyChanged(string) method not found: NoRaisePropertyChangedMethod.", 10, 19, 10, 47),
                        error => AssertError(error, Path.GetFullPath(name2), Messages.POCO_RaisePropertyChangedMethodNotFound.FullId, 13, 19, endColumnNumber: 43),
                        error => AssertError(error, Path.GetFullPath(name2), Messages.POCO_RaisePropertyChangedMethodNotFound.FullId, 17, 19, endColumnNumber: 41),
                        error => AssertError(error, Path.GetFullPath(name2), Messages.POCO_RaisePropertyChangedMethodNotFound.FullId, 21, 19, endColumnNumber: 39),
                        error => AssertError(error, Path.GetFullPath(name2), Messages.POCO_RaisePropertyChangedMethodNotFound.FullId, 25, 19, endColumnNumber: 47)
                )
            );
        }

        [Fact]
        public void CompleteViewModel_Errors2() {
            string incomplete1 =
@"
using MetaSharp;
using DevExpress.Mvvm.DataAnnotations;
namespace MetaSharp.Incomplete {
    public class POCOViewModel_SealedPropertyBase {
        public virtual int Property { get; set; }
    }
    public class POCOViewModel : POCOViewModel_FinalPropertyBase {
        [BindableProperty]
        public sealed override int SealedProperty { get; set; }

        [BindableProperty(OnPropertyChangedMethodName =""Abc"")]
        public virtual int InvalidOnPropertyChangedMethod { get; set; }
        protected void OnInvalidOnPropertyChangedMethodChanged(double oldValue) { }
    }
    public class InvalidIPOCOViewModelImplementation : DevExpress.Mvvm.POCO.IPOCOViewModel {
        void IPOCOViewModel.RaisePropertyChanged(string propertyName) {
            throw new NotImplementedException();
        }
    }
    public class POCOViewModel2 : POCOViewModel_FinalPropertyBase {
        [BindableProperty(OnPropertyChangingMethodName =""Abc"")]
        public virtual int InvalidOnPropertyChangingMethod { get; set; }
        protected void OnInvalidOnPropertyChangingMethodChanging(double oldValue) { }
    }
}";
            
            var name1 = "IncompleteViewModels1.cs";
            var input = GetInput(new[] { name1 }, defaultAttributes: ", new[] { new MetaCompleteViewModelAttribute() }");
            AssertMultipleFilesErrors(
                ImmutableArray.Create(
                    new TestFile(SingleInputFileName, input),
                    new TestFile(name1, incomplete1, isInFlow: false)
                ),
                errors => Assert.Collection(errors,
                        error => AssertError(error, Path.GetFullPath(name1), Messages.POCO_PropertyIsSealed.FullId,
                            "Cannot override sealed property: SealedProperty.", 10, 36, 10, 50),
                        error => AssertError(error, Path.GetFullPath(name1), Messages.POCO_PropertyChangedMethodNotFound(Chang.ed).FullId,
                            "Property changed method not found: Abc.", 13, 28, 13, 58),

                        error => AssertError(error, Path.GetFullPath(name1), Messages.POCO_TypeImplementsIPOCOViewModel.FullId,
                            "Type should not implement IPOCOViewModel: InvalidIPOCOViewModelImplementation.", 16, 18, 16, 53),

                        error => AssertError(error, Path.GetFullPath(name1), Messages.POCO_PropertyChangedMethodNotFound(Chang.ing).FullId,
                            "Property changing method not found: Abc.", 23, 28, 23, 59)
                )
            );
        }

        [Fact]
        public void CompleteViewModel_Errors3() {
            string incomplete1 =
@"
using MetaSharp;
using DevExpress.Mvvm.DataAnnotations;
namespace MetaSharp.Incomplete {
    public class POCOViewModel {
        public void Show() { }
        int ShowCommand = 0;

        public void Show2() { }
        public static event EventHandler Show2Command;

        [Command]
        public void TooMuchArgumentsMethod(int a, int b) { }

        [Command]
        public void OutParameter(out int a) { a = 0; }

        [Command]
        public void RefParameter(ref int a) { }

        [Command]
        public void CanExecuteParameterCountMismatch() { }
        public bool CanCanExecuteParameterCountMismatch(int a) { return true; }

        [Command]
        public void CanExecuteParameterTypeMismatch(long a) { }
        public bool CanCanExecuteParameterTypeMismatch(int a) { return true; }

        [Command]
        public void CanExecuteParameterTypeMismatch2(int a) { }
        public bool CanCanExecuteParameterTypeMismatch2(ref int a) { return true; }

        [Command(CanExecuteMethodName = ""Abc"")]
        public void InvalidCanExecuteName() { }
    }
}";

            var name1 = "IncompleteViewModels1.cs";
            var input = GetInput(new[] { name1 }, defaultAttributes: ", new[] { new MetaCompleteViewModelAttribute() }");
            AssertMultipleFilesErrors(
                ImmutableArray.Create(
                    new TestFile(SingleInputFileName, input),
                    new TestFile(name1, incomplete1, isInFlow: false)
                ),
                errors => Assert.Collection(errors,
                        error => AssertError(error, Path.GetFullPath(name1), Messages.POCO_MemberWithSameCommandNameAlreadyExists.FullId,
                            "Member with the same command name already exists: Show.", 6, 21, 6, 25),
                        error => AssertError(error, Path.GetFullPath(name1), Messages.POCO_MemberWithSameCommandNameAlreadyExists.FullId,
                            "Member with the same command name already exists: Show2.", 9, 21, 9, 26),
                        error => AssertError(error, Path.GetFullPath(name1), Messages.POCO_MethodCannotHaveMoreThanOneParameter.FullId,
                            "Method cannot have more than one parameter: TooMuchArgumentsMethod.", 13, 21, 13, 43),
                        error => AssertError(error, Path.GetFullPath(name1), Messages.POCO_MethodCannotHaveOutORRefParameters.FullId,
                            "Method cannot have out or reference parameter: OutParameter.", 16, 21, 16, 33),
                        error => AssertError(error, Path.GetFullPath(name1), Messages.POCO_MethodCannotHaveOutORRefParameters.FullId,
                            "Method cannot have out or reference parameter: RefParameter.", 19, 21, 19, 33),
                        error => AssertError(error, Path.GetFullPath(name1), Messages.POCO_CanExecuteMethodHasIncorrectParameters.FullId,
                            "CanExecute method has incorrect parameters: CanCanExecuteParameterCountMismatch.", 23, 21, 23, 56),
                        error => AssertError(error, Path.GetFullPath(name1), Messages.POCO_CanExecuteMethodHasIncorrectParameters.FullId,
                            "CanExecute method has incorrect parameters: CanCanExecuteParameterTypeMismatch.", 27, 21, 27, 55),
                        error => AssertError(error, Path.GetFullPath(name1), Messages.POCO_CanExecuteMethodHasIncorrectParameters.FullId,
                            "CanExecute method has incorrect parameters: CanCanExecuteParameterTypeMismatch2.", 31, 21, 31, 56),
                        error => AssertError(error, Path.GetFullPath(name1), Messages.POCO_MethodNotFound.FullId,
                            "Method not found: Abc.", 34, 21, 34, 42)

                )
            );
        }

        #endregion

        #region dependency properties
        [Fact]
        public void DependencyProperties() {
            string incomplete =
@"
using MetaSharp;
namespace MetaSharp.Incomplete {
using System;
    [MetaCompleteDependencyProperties]
    public partial class DObject {
        static DObject() {
            DoBefore();
            if(true) {
                DependencyPropertyRegistrator<DObject>.New()
                    .Register(x => x.No, out NProperty)
                ;
            }
            Some<X>.New().Register<string>(x => x.No, out NoProperty);
            DependencyPropertyRegistrator<X>.Bla().Register<string>(x => x.No, out NoProperty);
            DependencyPropertyRegistrator< DObject >. New ()
                .Register< string >(x => x. Prop1 , out  Prop1Property , null)
                .SomethingUnknownGeneric<T>()
                .SomethingUnknown()
                .RegisterReadOnly(x => x.Prop2, out Prop2PropertyKey, out Prop2Property, 3)
            ;
            DependencyPropertyRegistrator<DObject>.New()
                .RegisterAttached((FrameworkElement x) => GetProp3(x), out Prop3Property, string.Empty)
                .RegisterAttachedReadOnly<UIElement, string>(x => GetProp4(x), out Prop4PropertyKey, out Prop4Property, 5)
                .Register(x => x.Prop5, out Prop5Property, default(Some))
                .Register(x => x.Prop6, out Prop6Property, (string)GetSome())
            ;
            DoAfter();
        }
        public DObject() {
            DependencyPropertyRegistrator<DObject>.New()
                .Register(x => x.Prop1, out Prop1Property)
                .Register(x => x.Prop2, out Prop2Property)
            ;
        }
    }
    [MetaCompleteDependencyProperties]
    public class NoStaticCtor { 
    }
    [MetaCompleteDependencyProperties]
    public class NoRegistratorInStaticCtor { 
        static NoRegistratorInStaticCtor() {
        }
    }
    [MetaCompleteDependencyProperties]
    public class NoPropertiesGenerated { 
        static NoPropertiesGenerated() {
            DependencyPropertyRegistrator<NoPropertiesGenerated>.New()
                .BlaBla()
            ;
        }
    }
}";

            string output =
@"namespace MetaSharp.Incomplete {
using System;
    partial class DObject {
        public static readonly DependencyProperty Prop1Property;
        public string Prop1 {
            get { return (string)GetValue(Prop1Property); }
            set { SetValue(Prop1Property, value); }
        }
        public static readonly DependencyProperty Prop2Property;
        static readonly DependencyPropertyKey Prop2PropertyKey;
        public int Prop2 {
            get { return (int)GetValue(Prop2Property); }
            private set { SetValue(Prop2PropertyKey, value); }
        }
        public static readonly DependencyProperty Prop3Property;
        public static string GetProp3(FrameworkElement d) {
            return (string)d.GetValue(Prop3Property);
        }
        public static void SetProp3(FrameworkElement d, string value) {
            d.SetValue(Prop3Property, value);
        }
        public static readonly DependencyProperty Prop4Property;
        static readonly DependencyPropertyKey Prop4PropertyKey;
        public static string GetProp4(UIElement d) {
            return (string)d.GetValue(Prop4Property);
        }
        static void SetProp4(UIElement d, string value) {
            d.SetValue(Prop4PropertyKey, value);
        }
        public static readonly DependencyProperty Prop5Property;
        public Some Prop5 {
            get { return (Some)GetValue(Prop5Property); }
            set { SetValue(Prop5Property, value); }
        }
        public static readonly DependencyProperty Prop6Property;
        public string Prop6 {
            get { return (string)GetValue(Prop6Property); }
            set { SetValue(Prop6Property, value); }
        }
    }
}";
            var name = "IncompleteDObjects.cs";
            AssertMultipleFilesOutput(
                ImmutableArray.Create(
                    new TestFile(SingleInputFileName, GetInput(@"IncompleteDObjects.cs".Yield())),
                    new TestFile(name, incomplete, isInFlow: false)
                ),
                ImmutableArray.Create(
                    new TestFile(Path.Combine(DefaultIntermediateOutputPath, "IncompleteDObjects.g.i.cs"), output)
                ),
                ignoreEmptyLines: true
            );
            //AssertCompiles(input, incomplete, output, additionalClasses);
        }
        [Fact]
        public void ServiceTemplateProperty() {
            string incomplete =
@"
using MetaSharp;
namespace MetaSharp.Incomplete {
using System;
    [MetaCompleteDependencyProperties]
    public partial class DObject {
        static DObject() {
            DependencyPropertyRegistrator<DObject>.New()
                .RegisterServiceTemplateProperty(x => x.XServiceTemplate, out XServiceTemplateProperty, out xServiceAccessor)
            ;
        }
    }
}";

            string output =
@"namespace MetaSharp.Incomplete {
using System;
    partial class DObject {
        public static readonly DependencyProperty XServiceTemplateProperty;
        public DataTemplate XServiceTemplate {
            get { return (DataTemplate)GetValue(XServiceTemplateProperty); }
            set { SetValue(XServiceTemplateProperty, value); }
        }
    }
}";
            var name = "IncompleteDObjects.cs";
            AssertMultipleFilesOutput(
                ImmutableArray.Create(
                    new TestFile(SingleInputFileName, GetInput(@"IncompleteDObjects.cs".Yield())),
                    new TestFile(name, incomplete, isInFlow: false)
                ),
                ImmutableArray.Create(
                    new TestFile(Path.Combine(DefaultIntermediateOutputPath, "IncompleteDObjects.g.i.cs"), output)
                ),
                ignoreEmptyLines: true
            );
        }
        [Fact]
        public void BindableReadOnlyProperty() {
            string incomplete =
@"
using MetaSharp;
namespace MetaSharp.Incomplete {
using System;
    [MetaCompleteDependencyProperties]
    public partial class DObject {
        static DObject() {
            DependencyPropertyRegistrator<DObject>.New()
                .RegisterBindableReadOnly(x => x.Result, out setResult, out ResultProperty, default(Some))
            ;
        }
    }
}";

            string output =
@"namespace MetaSharp.Incomplete {
using System;
    partial class DObject {
        public static readonly DependencyProperty ResultProperty;
        public Some Result {
            get { return (Some)GetValue(ResultProperty); }
            [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
            set { SetValue(ResultProperty, value); }
        }
    }
}";
            var name = "IncompleteDObjects.cs";
            AssertMultipleFilesOutput(
                ImmutableArray.Create(
                    new TestFile(SingleInputFileName, GetInput(@"IncompleteDObjects.cs".Yield())),
                    new TestFile(name, incomplete, isInFlow: false)
                ),
                ImmutableArray.Create(
                    new TestFile(Path.Combine(DefaultIntermediateOutputPath, "IncompleteDObjects.g.i.cs"), output)
                ),
                ignoreEmptyLines: true
            );
        }
        [Fact]
        public void DependencyProperties_MissingPropertyType() {
            string incomplete =
@"
using MetaSharp;
namespace MetaSharp.Incomplete {
using System;
    [MetaCompleteDependencyProperties]
    public partial class DObject {
        static DObject() {
            DependencyPropertyRegistrator<DObject>.New()
                .Register(x => x.Prop1, out Prop1Property, x => 5)
                 .Register(x => x.Prop2, out Prop2Property, null)
            ;
        }
    }
}";
            var name = "IncompleteDObjects.cs";
            AssertMultipleFilesErrors(
                ImmutableArray.Create(
                    new TestFile(SingleInputFileName, GetInput((@"Some\..\" + name).Yield())),
                    new TestFile(@"Some\..\" + name, incomplete, isInFlow: false)
                ),
                errors => Assert.Collection(errors,
                        error => AssertError(error, Path.GetFullPath(name), Messages.DependecyProperty_PropertyTypeMissed.FullId, Messages.DependecyProperty_PropertyTypeMissed.Text, 9, 26),
                        error => AssertError(error, Path.GetFullPath(name), Messages.DependecyProperty_PropertyTypeMissed.FullId, Messages.DependecyProperty_PropertyTypeMissed.Text, 10, 27)
                )
            );
        }
        [Fact]
        public void DependencyProperties_InvalidFieldName() {
            string incomplete =
@"
using MetaSharp;
namespace MetaSharp.Incomplete {
using System;
    [MetaCompleteDependencyProperties]
    public partial class DObject {
        static DObject() {
            DependencyPropertyRegistrator<DObject>.New()
                .Register(x => x. Prop1 , out Prop2Property, string.Empty)
                .Register(x => x.Prop2, out Prop2Property_, 5)
                .RegisterReadOnly(x => x.Prop3, out Prop3Property, out Prop2Property, 5)
                .RegisterReadOnly(x => x.Prop4, out Prop4PropertyKey, out Prop3Property, 5)
            ;
        }
    }
}";
            var name = "IncompleteDObjects.cs";
            AssertMultipleFilesErrors(
                ImmutableArray.Create(
                    new TestFile(SingleInputFileName, GetInput(@"IncompleteDObjects.cs".Yield())),
                    new TestFile(name, incomplete, isInFlow: false)
                ),
                errors => Assert.Collection(errors,
                        error => AssertError(error, Path.GetFullPath(name), Messages.DependecyProperty_IncorrectPropertyName.FullId,
                            "Dependency property field for the the property 'Prop1' should have 'Prop1Property' name.", 9, 47, 9, 60),
                        error => AssertError(error, Path.GetFullPath(name), Messages.DependecyProperty_IncorrectPropertyName.FullId,
                            "Dependency property field for the the property 'Prop2' should have 'Prop2Property' name.", 10, 45, 10, 59),
                        error => AssertError(error, Path.GetFullPath(name), Messages.DependecyProperty_IncorrectPropertyName.FullId,
                            "Dependency property field for the the property 'Prop3' should have 'Prop3PropertyKey' name.", 11, 53, 11, 66),
                        error => AssertError(error, Path.GetFullPath(name), Messages.DependecyProperty_IncorrectPropertyName.FullId,
                            "Dependency property field for the the property 'Prop4' should have 'Prop4Property' name.", 12, 75, 12, 88)
                )
            );
        }
        [Fact]
        public void AttachedDependencyProperties_InvalidDedicatedAccessorMethodName() {
            string incomplete =
@"
using MetaSharp;
namespace MetaSharp.Incomplete {
using System;
    [MetaCompleteDependencyProperties]
    public partial class DObject {
        static DObject() {
            DependencyPropertyRegistrator<DObject>.New()
                .RegisterAttached((DependencyObject x) => Get(x), out Property, string.Empty)
                .RegisterAttachedReadOnly((DependencyObject x) => GeProp2(x), out Prop2PropertyKey, out Prop2Property, 5)
            ;
        }
    }
}";
            var name = "IncompleteDObjects.cs";
            AssertMultipleFilesErrors(
                ImmutableArray.Create(
                    new TestFile(SingleInputFileName, GetInput(@"IncompleteDObjects.cs".Yield())),
                    new TestFile(name, incomplete, isInFlow: false)
                ),
                errors => Assert.Collection(errors,
                        error => AssertError(error, Path.GetFullPath(name), Messages.DependecyProperty_IncorrectAttachedPropertyGetterName.FullId,
                            "Attached dependency property dedicated accessor method name should starts with 'Get' prefix: Get.", 9, 35, 9, 65),
                        error => AssertError(error, Path.GetFullPath(name), Messages.DependecyProperty_IncorrectAttachedPropertyGetterName.FullId,
                            "Attached dependency property dedicated accessor method name should starts with 'Get' prefix: GeProp2.", 10, 43, 10, 77)
                )
            );
        }
        [Fact]
        public void DependencyProperties_InvalidType() {
            string incomplete =
@"
using MetaSharp;
namespace MetaSharp.Incomplete {
using System;
    [MetaCompleteDependencyProperties]
    public partial class DObject {
        static DObject() {
            DependencyPropertyRegistrator< Bla.DObject  >.New()
                .Register(x => x. Prop1 , out Prop1Property, string.Empty)
            ;
        }
    }
}";
            var name = "IncompleteDObjects.cs";
            AssertMultipleFilesErrors(
                ImmutableArray.Create(
                    new TestFile(SingleInputFileName, GetInput(@"IncompleteDObjects.cs".Yield())),
                    new TestFile(name, incomplete, isInFlow: false)
                ),
                errors => Assert.Collection(errors,
                        error => AssertError(error, Path.GetFullPath(name), Messages.DependecyProperty_IncorrectOwnerType.FullId,
                            Messages.DependecyProperty_IncorrectOwnerType.Text, 8, 44, 8, 55)
                )
            );
        }
        [Fact]
        public void DependencyProperties_DPAccessModifierAttribute() {
            string incomplete =
@"
using MetaSharp;
namespace MetaSharp.Incomplete {
using System;
    [MetaCompleteDependencyProperties]
    public partial class DObject {
        public static readonly DependencyProperty Prop3Property;
        public static readonly DependencyProperty Prop5Property;
        [DPAccessModifier(MemberVisibility.Internal)]
        public static readonly DependencyProperty Prop4Property;
        [DPAccessModifierAttribute(MemberVisibility.Private, MemberVisibility.Private)]
        static readonly DependencyProperty Prop6Property;

        static DObject() {
            DependencyPropertyRegistrator<DObject>.New()
                .RegisterAttached((FrameworkElement x) => GetProp3(x), out Prop3Property, string.Empty)
                .RegisterAttachedReadOnly<UIElement, string>(x => GetProp4(x), out Prop4PropertyKey, out Prop4Property, 5)
                .Register(x => x.Prop5, out Prop5Property, default(Some))
                .Register(x => x.Prop6, out Prop6Property, (string)GetSome())
            ;
        }
    }
    [AttributeUsage(AttributeTargets.Field)]
    sealed class DPAccessModifierAttribute : Attribute {
        public DPAccessModifierAttribute(MemberVisibility setterVisibility, MemberVisibility getterVisibility = MemberVisibility.Public) {
            SetterVisibility = setterVisibility;
            GetterVisibility = getterVisibility;
        }
        public MemberVisibility SetterVisibility { get; set; }
        public MemberVisibility GetterVisibility { get; set; }
    }
    enum MemberVisibility {
        Public,
        Protected,
        Private,
        Internal,
        ProtectedInternal
    }
}";

            string output =
@"namespace MetaSharp.Incomplete {
using System;
    partial class DObject {
        public static readonly DependencyProperty Prop3Property;
        public static string GetProp3(FrameworkElement d) {
            return (string)d.GetValue(Prop3Property);
        }
        public static void SetProp3(FrameworkElement d, string value) {
            d.SetValue(Prop3Property, value);
        }
        static readonly DependencyPropertyKey Prop4PropertyKey;
        public static string GetProp4(UIElement d) {
            return (string)d.GetValue(Prop4Property);
        }
        internal static void SetProp4(UIElement d, string value) {
            d.SetValue(Prop4PropertyKey, value);
        }
        public static readonly DependencyProperty Prop5Property;
        public Some Prop5 {
            get { return (Some)GetValue(Prop5Property); }
            set { SetValue(Prop5Property, value); }
        }
        string Prop6 {
            get { return (string)GetValue(Prop6Property); }
            set { SetValue(Prop6Property, value); }
        }
    }
}";
            var name = "IncompleteDObjects.cs";
            AssertMultipleFilesOutput(
                ImmutableArray.Create(
                    new TestFile(SingleInputFileName, GetInput(@"IncompleteDObjects.cs".Yield())),
                    new TestFile(name, incomplete, isInFlow: false)
                ),
                ImmutableArray.Create(
                    new TestFile(Path.Combine(DefaultIntermediateOutputPath, "IncompleteDObjects.g.i.cs"), output)
                ),
                ignoreEmptyLines: true
            );
            //AssertCompiles(input, incomplete, output, additionalClasses);
        }
        [Fact]
        public void DependencyProperties_SimpleClassWithImplicitlyDeclaredStaticConstructor() {
            string incomplete =
@"
using System;
using MetaSharp;
namespace MetaSharp.Incomplete {
    [MetaCompleteDependencyProperties]
    public class SimpleObject {
        public static readonly SimpleObject Default = new SimpleObject();
    }
}";

            string output =
@"";
            var name = "IncompleteDObjects.cs";
            AssertMultipleFilesOutput(
                ImmutableArray.Create(
                    new TestFile(SingleInputFileName, GetInput(@"IncompleteDObjects.cs".Yield())),
                    new TestFile(name, incomplete, isInFlow: false)
                ),
                ImmutableArray.Create(
                    new TestFile(Path.Combine(DefaultIntermediateOutputPath, "IncompleteDObjects.g.i.cs"), output)
                ),
                ignoreEmptyLines: true
            );
            //AssertCompiles(input, incomplete, output, additionalClasses);
        }
        [Fact]
        public void DependencyProperties_GenericClass() {
            string incomplete =
@"
using MetaSharp;
namespace MetaSharp.Incomplete {
using System;
    [MetaCompleteDependencyProperties]
    public partial class DObject<T> where T : class, IClonable {
        static DObject() {
            DependencyPropertyRegistrator<DObject<T>>.New()
                .RegisterAttached((FrameworkElement x) => GetProp3(x), out Prop3Property, string.Empty)
                .RegisterAttachedReadOnly<UIElement, string>(x => GetProp4(x), out Prop4PropertyKey, out Prop4Property, 5)
                .Register(x => x.Prop5, out Prop5Property, default(Some))
                .Register(x => x.Prop6, out Prop6Property, (string)GetSome())
            ;
        }
    }
}";

            string output =
@"namespace MetaSharp.Incomplete {
using System;
    partial class DObject<T> {
        public static readonly DependencyProperty Prop3Property;
        public static string GetProp3(FrameworkElement d) {
            return (string)d.GetValue(Prop3Property);
        }
        public static void SetProp3(FrameworkElement d, string value) {
            d.SetValue(Prop3Property, value);
        }
        public static readonly DependencyProperty Prop4Property;
        static readonly DependencyPropertyKey Prop4PropertyKey;
        public static string GetProp4(UIElement d) {
            return (string)d.GetValue(Prop4Property);
        }
        static void SetProp4(UIElement d, string value) {
            d.SetValue(Prop4PropertyKey, value);
        }
        public static readonly DependencyProperty Prop5Property;
        public Some Prop5 {
            get { return (Some)GetValue(Prop5Property); }
            set { SetValue(Prop5Property, value); }
        }
        public static readonly DependencyProperty Prop6Property;
        public string Prop6 {
            get { return (string)GetValue(Prop6Property); }
            set { SetValue(Prop6Property, value); }
        }
    }
}";
            var name = "IncompleteDObjects.cs";
            AssertMultipleFilesOutput(
                ImmutableArray.Create(
                    new TestFile(SingleInputFileName, GetInput(@"IncompleteDObjects.cs".Yield())),
                    new TestFile(name, incomplete, isInFlow: false)
                ),
                ImmutableArray.Create(
                    new TestFile(Path.Combine(DefaultIntermediateOutputPath, "IncompleteDObjects.g.i.cs"), output)
                ),
                ignoreEmptyLines: true
            );
            //AssertCompiles(input, incomplete, output, additionalClasses);
        }
        #endregion

        static string GetInput(IEnumerable<string> protoFiles, string methodAttributes = null, string defaultAttributes = null, string assemblyAttributes = null) {
            var files = protoFiles.Select(x => "@\"" + x + "\"").ConcatStrings(", ");
            return
$@"
using System;
using MetaSharp;
using System.Collections.Generic;
{assemblyAttributes}
namespace MetaSharp.Incomplete {{
    public static class CompleteFiles {{
{methodAttributes}
        public static Either<IEnumerable<MetaError>, IEnumerable<Output>> Complete(MetaContext context) {{
            return context.Complete(new[] {{ {files} }} {defaultAttributes});
        }}
    }}
}}
";
        }
    }
}