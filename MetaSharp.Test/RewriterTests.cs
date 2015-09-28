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
        public void RewriteOnlyMethodsWithAttributes_RewriteMultypleTypeArgs() {
            var input = @"
namespace MetaSharp.HelloWorld {
    using System;
    public static class HelloWorldGenerator {
        public class ClassGenerator {
            [System.Runtime.InteropServices.ComVisible(true)] //mere attempt to distract rewriter
            public static ClassGenerator Class<T>() {
                return new ClassGenerator();
            }
            public ClassGenerator Property<T>(int number) {
                return this;
            }
            public string Generate(string text) {
                return ""I am not "" + text + ""!"";
            }
            [MetaRewriteTypeArgs]
            public static string TwoGenericArgs<T1, T2>([MetaRewriteLambdaParam] Func<T1, string> lambda1, [MetaRewriteParam] Func<T2, int> lambda2, int noRewrite) {
                throw new NotImplementedException();
            }
            public static string TwoGenericArgs(string t1, string t2, string t1Member, string t2Lambda, int noRewrite) {
                return t1 + "" "" + t2 + "" "" + t1Member + "" "" + t2Lambda + "" "" + noRewrite;
            }
            public static string NoGenericArgs([MetaRewriteParam] Func<string, int> lambda) {
                throw new NotImplementedException();
            }
            public static string NoGenericArgs(string val) {
                return val;
            }
        }
        public static string MakeFoo(MetaContext context) {
            var text = ""rewritten"";
            return ClassGenerator.Class<string>().Property<int>(42).Generate(text)
                + "" "" + ClassGenerator.TwoGenericArgs<Boo, Moo>(x => x.BooProp, x => x.MooProp, 42) 
                + "" "" + ClassGenerator.NoGenericArgs(x => x.Length);
        }
    }
}
";
            string additionalClasses = @"
namespace MetaSharp.HelloWorld {
    public class Boo {
        public string BooProp { get; set; }
    }
    public class Moo { 
        public int MooProp { get; set; }
    }
}";
            string output = "I am not rewritten! Boo Moo BooProp x => x.MooProp 42 x => x.Length";
            AssertSingleFileOutput(input, output);
            AssertCompiles(input, additionalClasses);
        }
        [Fact]
        public void MissingTypeArguments_TooManyTypeArguments() {
            var input = @"
namespace MetaSharp.HelloWorld {
    using System;
    public static class HelloWorldGenerator {
        public class ClassGenerator {
            [MetaRewriteTypeArgs]
            public static string Rewriteable<T>() {
                throw new NotImplementedException();
            }
            public static string Rewriteable(string t) {
                throw new NotImplementedException();
            }
        }
        public static string MakeFoo(MetaContext context) {
             return ClassGenerator.Rewriteable<>() + ClassGenerator.Rewriteable<string, int>();
        }
    }
}
";
            AssertSingleFileErrors(input, errors => {
                Assert.Collection(errors,
                    error => AssertError(error, SingleInputFileName, "CS0305"),
                    error => AssertError(error, SingleInputFileName, "CS0305"));
            });
        }

        //        [Fact]
        //        public void SandBox_______________________() {
        //            var input = @"
        //using MetaSharp;
        //namespace MetaSharp.HelloWorld {
        //    public static class HelloWorldGenerator {
        //        public static string MakeFoo(MetaContext context) {
        //             var classText = ClassGenerator.Class<Foo>()
        //                .Property<Boo>(x => x.IntProperty, default(Boo))
        //                .Generate();
        //            return context.WrapMembers(classText);
        //        }
        //    }
        //}
        //";
        //            string output =
        //@"namespace MetaSharp.HelloWorld {


        //    public class Foo {
        //        public Boo IntProperty { get; }
        //        public Foo(Boo intProperty = default(Boo)) {
        //        }
        //    }
        //}";

        //            AssertSingleFileOutput(input, output);
        //        }
    }
}
