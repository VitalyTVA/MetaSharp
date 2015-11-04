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
            string incomplete =
@"
using MetaSharp;
using DevExpress.Mvvm.DataAnnotations;
namespace MetaSharp.Incomplete {
    public class POCOViewModel_Errors {
        [BindableProperty]
        public string Property { get; set; }
        //[BindableProperty]
        //public virtual string Property { get { return null; } }
    }
}";

            var name = "IncompleteViewModels.cs";
            var input = GetInput(@"IncompleteViewModels.cs".Yield(), defaultAttributes: ", new[] { new MetaCompleteViewModelAttribute() }");
            AssertMultipleFilesErrors(
                ImmutableArray.Create(
                    new TestFile(SingleInputFileName, input),
                    new TestFile(name, incomplete, isInFlow: false)
                ),
                errors => Assert.Collection(errors,
                        error => AssertError(error, Path.GetFullPath(name), Messages.POCO_PropertyIsNotVirual.FullId,
                            "Cannot make non-virtual property bindable: Property.", 6, 9, 7, 45)
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
                .RegisterAttached(x => x.Prop3, out Prop3Property, string.Empty)
                .RegisterAttachedReadOnly<string>(x => x.Prop4, out Prop4PropertyKey, out Prop4Property, 5)
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
        public string GetProp3(DependencyObject d) {
            return (string)d.GetValue(Prop3Property);
        }
        public void SetProp3(DependencyObject d, string value) {
            d.SetValue(Prop3Property, value);
        }
        public static readonly DependencyProperty Prop4Property;
        static readonly DependencyPropertyKey Prop4PropertyKey;
        public string GetProp4(DependencyObject d) {
            return (string)d.GetValue(Prop4Property);
        }
        void SetProp4(DependencyObject d, string value) {
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