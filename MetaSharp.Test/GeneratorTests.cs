using MetaSharp.Tasks;
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
            AssertSingleFile(input, output);
        }


    }
    public class GeneratorTestsBase {
        class TestEnvironment {
            public readonly Dictionary<string, string> Files;
            public readonly Environment Environment;
            public TestEnvironment(Dictionary<string, string> files, Environment environment) {
                Files = files;
                Environment = environment;
            }
        }
        protected static void AssertSingleFile(string input, string output) {
            const string inputFileName = "file.meta.cs";
            const string outputFileName = "obj\\file.meta.g.i.cs";

            var testEnvironment = CreateEnvironment();
            testEnvironment.Environment.WriteText(inputFileName, input);
            var result = Generator.Generate(ImmutableArray.Create(inputFileName), testEnvironment.Environment, PlatformEnvironment.DefaultReferences);
            Assert.Empty(result.Errors);
            Assert.Equal<string>(ImmutableArray.Create(outputFileName), result.Files);
            Assert.Equal(2, testEnvironment.Files.Count);
            Assert.Equal(input, testEnvironment.Files[inputFileName]);
            Assert.Equal(output, testEnvironment.Files[outputFileName]);
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

