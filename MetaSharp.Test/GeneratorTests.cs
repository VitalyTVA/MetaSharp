using MetaSharp.Tasks;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reflection;
using Xunit;

namespace MetaSharp.Test {
    public class HelloWorldTests : GeneratorTestsBase {
        [Fact]
        public void HelloWorld() {
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
        public void HelloWorld_NonPublicClass() {
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
        public void HelloWorld_NonPublicMethod() {
            var input = @"
namespace MetaSharp.HelloWorld.NonPublicClass {
    static class HelloWorldGenerator_NonPublicClass {
        internal static string SayHelloAgain() {
             return ""Hello World!"";
        }
    }
}
";
            var output = "Hello World!";
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

