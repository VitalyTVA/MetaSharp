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
        public static string MakeFoo() {
             return ClassGenerator.Class<Foo>().Generate();
        }
        public static string MakeMoo() {
             return ClassGenerator.Class<Moo>().Generate();
        }
    }
}
";
            var input2 = @"
using MetaSharp;
namespace MetaSharp.HelloWorld {
    public static class HelloWorldGenerator2 {
        public static string MakeBoo() {
             return ClassGenerator.Class<Boo>().Generate();
        }
    }
}
";
            string output1 =
@"    public class Foo {
    }
    public class Moo {
    }";
            string output2 =
@"    public class Boo {
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
        }
    }
}
