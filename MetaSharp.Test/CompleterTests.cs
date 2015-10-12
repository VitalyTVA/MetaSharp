using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace MetaSharp.Test {
    public class CompleterTests : GeneratorTestsBase {
        #region class
        [Fact]
        public void CompletePrototypeFiles() {
            var input = @"
using MetaSharp;
[assembly: MetaProto(""IncompleteClasses1.cs"")]
[assembly: MetaProto(""IncompleteClasses2.cs"", MetaLocationKind.Designer)]

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
                    new TestFile(GetProtoOutputFileName(name1), output1),
                    new TestFile(GetProtoOutputFileNameDesigner(name2), output2, isInFlow: false)
                ),
                ignoreEmptyLines: true
            );
            AssertCompiles(input, incomplete1, incomplete2, output1, output2, additionalClasses);
        }
        [Fact]
        public void CompletePrototypeFiles_TypeNameWithNameSpace_ShortName_Alias() {
            var input = @"
using MetaSharp;
[assembly: MetaProto(""IncompleteClasses.cs"")]
";
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
            AssertMultipleFilesOutput(
                ImmutableArray.Create(
                    new TestFile(SingleInputFileName, input),
                    new TestFile(name, incomplete, isInFlow: false)
                ),
                ImmutableArray.Create(
                    new TestFile(GetProtoOutputFileName(name), output)
                ),
                ignoreEmptyLines: true
            );
            AssertCompiles(input, incomplete, output, additionalClasses);
        }
        #endregion

        #region view model
        [Fact]
        public void CompleteViewModel() {
            var input = @"
using MetaSharp;
[assembly: MetaProto(""IncompleteViewModels.cs"")]
";
            string incomplete =
@"
using MetaSharp;
namespace MetaSharp.Incomplete {
using System;
    [MetaCompleteViewModel]
    public partial class ViewModel {
        public virtual Boo BooProperty { get; set; }
        public virtual int IntProperty { get; set; }
    }
}";

            string output =
@"namespace MetaSharp.Incomplete {
using System;
    using System.ComponentModel;
    partial class ViewModel {
        public static ViewModel Create() {
            return new ViewModelImplementation();
        }
        class ViewModelImplementation : ViewModel, INotifyPropertyChanged {
            public override Boo BooProperty {
                get { return base.BooProperty; }
                set {
                    if(base.BooProperty == value)
                        return;
                    base.BooProperty = value;
                    RaisePropertyChanged(""BooProperty"");
                }
            }
            public override int IntProperty {
                get { return base.IntProperty; }
                set {
                    if(base.IntProperty == value)
                        return;
                    base.IntProperty = value;
                    RaisePropertyChanged(""IntProperty"");
                }
            }
            public event PropertyChangedEventHandler PropertyChanged;
            void RaisePropertyChanged(string property) {
                var handler = PropertyChanged;
                if(handler != null)
                    handler(this, new PropertyChangedEventArgs(property));
            }
        }
    }
}";
            string additionalClasses = @"
namespace MetaSharp.Incomplete {
    public class Boo {
    }
}";
            var name = "IncompleteViewModels.cs";
            AssertMultipleFilesOutput(
                ImmutableArray.Create(
                    new TestFile(SingleInputFileName, input),
                    new TestFile(name, incomplete, isInFlow: false)
                ),
                ImmutableArray.Create(
                    new TestFile(GetProtoOutputFileName(name), output)
                ),
                ignoreEmptyLines: true
            );
            AssertCompiles(input, incomplete, output, additionalClasses);
        }
        #endregion

        #region view model
        [Fact]
        public void DependencyProperties() {
            var input = @"
using MetaSharp;
[assembly: MetaProto(""IncompleteDObjects.cs"")]
";
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
            DependencyPropertyRegistrator<DObject>. New ()
                .Register<string>(x => x.Prop1, out Prop1Property, x => 5)
                .SomethingUnknownGeneric<T>()
                .SomethingUnknown()
                .RegisterReadOnly<int>(x => x.Prop2, out Prop2Property, out Prop2PropertyKey, 3)
            ;
            DependencyPropertyRegistrator<DObject>.New()
                .RegisterAttached<string>(x => x.Prop3, out Prop3Property)
                .RegisterAttachedReadOnly<string>(x => x.Prop4, out Prop4Property, out Prop4PropertyKey)
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
    }
}";
            var name = "IncompleteDObjects.cs";
            AssertMultipleFilesOutput(
                ImmutableArray.Create(
                    new TestFile(SingleInputFileName, input),
                    new TestFile(name, incomplete, isInFlow: false)
                ),
                ImmutableArray.Create(
                    new TestFile(GetProtoOutputFileName(name), output)
                ),
                ignoreEmptyLines: true
            );
            //AssertCompiles(input, incomplete, output, additionalClasses);
        }
        [Fact]
        public void DependencyProperties_MissingPropertyType() {
            var input = @"
using MetaSharp;
[assembly: MetaProto(""IncompleteDObjects.cs"")]
";
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
                 .Register(x => x.Prop2, out Prop2Property, x => 5)
            ;
        }
    }
}";
            var name = "IncompleteDObjects.cs";
            AssertMultipleFilesErrors(
                ImmutableArray.Create(
                    new TestFile(SingleInputFileName, input),
                    new TestFile(name, incomplete, isInFlow: false)
                ),
                errors => Assert.Collection(errors,
                        error => AssertError(error, name, DependencyPropertiesCompleter.PropertyTypeMissed_Id, DependencyPropertiesCompleter.PropertyTypeMissed_Message, 9, 26),
                        error => AssertError(error, name, DependencyPropertiesCompleter.PropertyTypeMissed_Id, DependencyPropertiesCompleter.PropertyTypeMissed_Message, 10, 27)
                )
            );
        }
        #endregion

    }
}