using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace MetaSharp.Test {
    public class CompleterTests : GeneratorTestsBase {
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
    using System;
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
    using System;
    [MetaCompleteClass]
    public partial class Foo3 {
        public Boo BooProperty { get; }
    }
}";

            string output1 =
@"namespace MetaSharp.Incomplete {
using FooBoo;
using System;
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
using System;
    partial class Foo2 {
        public Foo2(Boo booProperty) {
            BooProperty = booProperty;
        }
    }
}";
            string output2 =
@"namespace MetaSharp.Incomplete {
using FooBoo;
using System;
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
        public void CompletePrototypeFiles_TypeNameWithNameSpaceAndShortName() {
            var input = @"
using MetaSharp;
[assembly: MetaProto(""IncompleteClasses.cs"")]
";
            string incomplete =
@"
using MetaSharp;
namespace MetaSharp.Incomplete {
    using System;
    [MetaCompleteClass]
    public partial class Foo {
        public FooBoo.Boo BooProperty { get; }
        public Int32 IntProperty { get; }
    }
}";

            string output =
@"namespace MetaSharp.Incomplete {
using System;
    partial class Foo {
        public Foo(FooBoo.Boo booProperty, int intProperty) {
            BooProperty = booProperty;
            IntProperty = intProperty;
        }
    }
}";
            string additionalClasses = @"
namespace FooBoo {
    public class Boo {
        public string BooProp { get; set; }
    }
}";
            var name1 = "IncompleteClasses.cs";
            AssertMultipleFilesOutput(
                ImmutableArray.Create(
                    new TestFile(SingleInputFileName, input),
                    new TestFile(name1, incomplete, isInFlow: false)
                ),
                ImmutableArray.Create(
                    new TestFile(GetProtoOutputFileName(name1), output)
                ),
                ignoreEmptyLines: true
            );
            AssertCompiles(input, incomplete, output, additionalClasses);
        }

    }
}