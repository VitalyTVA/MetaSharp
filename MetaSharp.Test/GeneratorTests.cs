using MetaSharp.Tasks;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reflection;
using Xunit;

namespace MetaSharp.Test {
    public class HelloWorldTests : GeneratorTestsBase {
        [Fact]
        public void Default() {
            var input = @"
namespace MetaSharp.HelloWorld {
    public static class HelloWorldGenerator {
        public static string SayHello() {
             return ""Hello World!"";
        }
    }
}
";
            var output = "Hello World!";
            AssertSingleFileSimpleOutput(input, output);
        }
        [Fact]
        public void NonPublicClass() {
            var input = @"
namespace MetaSharp.HelloWorld.NonPublicClass {
    static class HelloWorldGenerator_NonPublicClass {
        public static string SayHelloAgain() {
             return ""Hello World!"";
        }
    }
}
";
            var output = "Hello World!";
            AssertSingleFileSimpleOutput(input, output);
        }
        [Fact]
        public void NonPublicMethod() {
            var input = @"
namespace MetaSharp.HelloWorld.NonPublicClass {
    static class HelloWorldGenerator_NonPublicClass {
        internal static string SayHelloAgain() {
             return ""Hello World!"";
        }
    }
}
";
            var output = string.Empty;
            AssertSingleFileSimpleOutput(input, output);
        }
        [Fact]
        public void SeveralMethods() {
            var input = @"
using System;
namespace MetaSharp.HelloWorld {
    public static class HelloWorldGenerator {
        public static string SayHello() {
             return ""Hello World!"";
        }
        public static string SayHelloAgain() {
             return ""Hello World Again!"";
        }
        static string EpicFail() {
             throw new NotImplementedException();
        }
    }
}
";
            var output = "Hello World!\r\nHello World Again!";
            AssertSingleFileSimpleOutput(input, output);
        }

        static void AssertSingleFileSimpleOutput(string input, string output) {
            AssertSingleFileOutput(input, output);
        }


    }
    public class GeneratorTestsBase {
        protected class TestEnvironment {
            public readonly Environment Environment;

            readonly Dictionary<string, string> files;
            public int FileCount => files.Count;
            public string ReadText(string fileName) => files[fileName];

            public TestEnvironment(Dictionary<string, string> files, Environment environment) {
                this.files = files;
                Environment = environment;
            }
        }
        protected static void AssertSingleFileResult(string input, Action<GeneratorResult, TestEnvironment> assertion) {
            const string inputFileName = "file.meta.cs";
            var testEnvironment = CreateEnvironment();
            testEnvironment.Environment.WriteText(inputFileName, input);
            var result = Generator.Generate(ImmutableArray.Create(inputFileName), testEnvironment.Environment, PlatformEnvironment.DefaultReferences);
            Assert.Equal(input, testEnvironment.ReadText(inputFileName));
            assertion(result, testEnvironment);
        }
        protected static void AssertSingleFileOutput(string input, string output) {
            AssertSingleFileResult(input, (result, testEnvironment) => {
                Assert.Empty(result.Errors);

                const string outputFileName = "obj\\file.meta.g.i.cs";
                Assert.Equal<string>(ImmutableArray.Create(outputFileName), result.Files);
                Assert.Equal(2, testEnvironment.FileCount);
                Assert.Equal(output, testEnvironment.ReadText(outputFileName));
            });
        }

        static TestEnvironment CreateEnvironment() {
            var files = new Dictionary<string, string>();
            var environment = PlatformEnvironment.Create(
                fileName => files[fileName],
                (fileName, text) => files[fileName] = text,
                "obj"
            );
            return new TestEnvironment(files, environment);
        }
    }
}

