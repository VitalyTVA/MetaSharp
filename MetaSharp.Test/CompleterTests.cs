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
[assembly: MetaProtoAttribute(""IncompleteClasses.cs"")]

namespace MetaSharp.HelloWorld {
    public partial class NoCompletion {
        public int IntProperty { get; }
    }
}
";
            string incomplete =
@"
using MetaSharp;
namespace MetaSharp.Incomplete {
    using FooBoo;
    using System;
    [MetaCompleteClass]
    public partial class Foo {
        public Boo BooProperty { get; }
        public Moo MooProperty { get; }
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

            string output =
@"namespace MetaSharp.Incomplete {
using FooBoo;
using System;
    partial class Foo {
        public Foo(Boo booProperty, Moo mooProperty, Int32 intProperty) {
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
            string additionalClasses = @"
namespace FooBoo {
    public class Boo {
        public string BooProp { get; set; }
    }
    public class Moo { 
        public int MooProp { get; set; }
    }
}";
            var name = "IncompleteClasses.cs";
            AssertMultipleFilesOutput(
                ImmutableArray.Create(new TestFile(SingleInputFileName, input), new TestFile(name, incomplete, isInFlow: false)),
                ImmutableArray.Create(
                    new TestFile(GetProtoOutputFileName(name), output)
                ),
                ignoreEmptyLines: true
            );
            AssertCompiles(input, incomplete, output, additionalClasses);
        }
    }
}