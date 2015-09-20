using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace MetaSharp.Test {
    public class RewriterTests : GeneratorTestsBase {
        [Fact]
        public void RewriteClassName() {
            var input = @"
using MetaSharp;
namespace MetaSharp.HelloWorld {
    public static class HelloWorldGenerator {
        public static string MakeFoo() {
             return ClassGenerator.Class<Foo>();
        }
        public static string MakeMoo() {
             return ClassGenerator.Class<Moo>();
        }
    }
}
";
            string output =
@"    public class Foo {
    }
    public class Moo {
    }";
            AssertSingleFileSimpleOutput(input, output);
        }
    }
}
