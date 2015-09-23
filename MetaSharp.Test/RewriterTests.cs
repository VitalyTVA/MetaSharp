using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace MetaSharp.Test {
    public class RewriterTests : GeneratorTestsBase {
        [Fact]
        public void RewriteClassName() {
            var input1 = @"
using MetaSharp;
namespace MetaSharp.HelloWorld {
    public static class HelloWorldGenerator {
        public static string MakeFoo(MetaContext context) {
             return context.WrapMembers(ClassGenerator.Class<Foo>().Generate());
        }
        public static string MakeMoo(MetaContext context) {
             return context.WrapMembers(ClassGenerator.Class<Moo>().Generate());
        }
    }
}
";
            var input2 = @"
using MetaSharp;
namespace MetaSharp.HelloWorld {
    public static class HelloWorldGenerator2 {
        public static string MakeBoo(MetaContext context) {
             return context.WrapMembers(ClassGenerator.Class<Boo>().Generate());
        }
    }
}
";
            string output1 =
@"namespace MetaSharp.HelloWorld {


    public class Foo {

        public Foo() {
        }
    }
}
namespace MetaSharp.HelloWorld {


    public class Moo {

        public Moo() {
        }
    }
}";
            string output2 =
@"namespace MetaSharp.HelloWorld {


    public class Boo {

        public Boo() {
        }
    }
}";
            var name1 = "file1.meta.cs";
            var name2 = "file2.meta.cs";
            AssertMultipleFilesOutput(
                ImmutableArray.Create(new TestFile(name2, input2), new TestFile(name1, input1)),
                ImmutableArray.Create(
                    new TestFile(GetOutputFileName(name1), output1),
                    new TestFile(GetOutputFileName(name2), output2)
                )
            );
            AssertCompiles(input1, input2, output1, output2);
        }
        [Fact]
        public void RewriteProperties() {
            var input = @"
using MetaSharp;
namespace MetaSharp.HelloWorld {
    using System;
    public static class HelloWorldGenerator {
        public static string MakeFoo(MetaContext context) {
             var classText = ClassGenerator.Class<Foo>()
                .Property<Boo>(x => x.BooProperty)
                .Property<Moo>(y => y.MooProperty, default(Moo))
                .Property<int>((Foo x) => x.IntProperty, 117)
                .Generate();
            return context.WrapMembers(classText);
        }
    }
}
";
            string output =
@"namespace MetaSharp.HelloWorld {
using System;

    public class Foo {
        public Boo BooProperty { get; }
        public Moo MooProperty { get; }
        public int IntProperty { get; }
        public Foo(Boo booProperty, Moo mooProperty = default(Moo), int intProperty = 117) {
        }
    }
}";

            string additionalClasses = @"
namespace MetaSharp.HelloWorld {
    public class Boo { }
    public class Moo { }
}";
            AssertSingleFileOutput(input, output);
            AssertCompiles(input, output, additionalClasses);
        }

        [Fact]
        public void SandBox_______________________() {
            var input = @"
using MetaSharp;
namespace MetaSharp.HelloWorld {
    public static class HelloWorldGenerator {
        public static string MakeFoo(MetaContext context) {
             var classText = ClassGenerator.Class<Foo>()
                .Property<Boo>(x => x.IntProperty, default(Boo))
                .Generate();
            return context.WrapMembers(classText);
        }
    }
}
";
            string output =
@"namespace MetaSharp.HelloWorld {


    public class Foo {
        public Boo IntProperty { get; }
        public Foo(Boo intProperty = default(Boo)) {
        }
    }
}";

            AssertSingleFileOutput(input, output);
        }
    }
}
