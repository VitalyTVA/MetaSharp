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
        public void NullOutput() {
            var input = @"
namespace MetaSharp.HelloWorld {
    public static class HelloWorldGenerator {
        public static string SayHello() {
             return null;
        }
    }
}
";
            var output = "";
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
namespace MetaSharp.HelloWorld.NonPublicMethod {
    static class HelloWorldGenerator_NonPublicMethod {
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
        [Fact]
        public void CompilationError() {
            var input = @"
namespace MetaSharp.HelloWorld {
    public static class HelloWorldGenerator {
        public static string SayHello() {
             return ""Hello World!""
        }
    
}
";
            AssertSingleFileErrors(input, errors => {
                Assert.Collection(errors, 
                    error => AssertError(error, "CS1002", "; expected", 4, 34),
                    error => AssertError(error, "CS1513", "} expected", 7, 1));
            });
        }
        [Fact]
        public void SeveralClasses() {
            var input = @"
namespace MetaSharp.HelloWorld {
    public static class HelloWorldGenerator {
        public static string SayHello() {
             return ""Hello World!"";
        }
    }
    public static class HelloWorldGenerator2 {
        public static string SayHelloAgain() {
             return ""Hello World Again!"";
        }
        public static string SayHelloOneMoreTime() {
             return ""Hello World One More Time!"";
        }
    }
}
";
            var output = @"Hello World!

Hello World Again!
Hello World One More Time!";
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
        const string SingleInputFileName = "file.meta.cs";
        static void AssertSingleFileResult(string input, Action<GeneratorResult, TestEnvironment> assertion) {
            var testEnvironment = CreateEnvironment();
            testEnvironment.Environment.WriteText(SingleInputFileName, input);
            var result = Generator.Generate(ImmutableArray.Create(SingleInputFileName), testEnvironment.Environment, PlatformEnvironment.DefaultReferences);
            Assert.Equal(input, testEnvironment.ReadText(SingleInputFileName));
            assertion(result, testEnvironment);
        }
        protected static void AssertSingleFileOutput(string input, string output) {
            AssertSingleFileResult(input, (result, testEnvironment) => {
                Assert.Empty(result.Errors);

                const string SingleOutputFileName = "obj\\file.meta.g.i.cs";
                Assert.Equal<string>(ImmutableArray.Create(SingleOutputFileName), result.Files);
                Assert.Equal(2, testEnvironment.FileCount);
                Assert.Equal(output, testEnvironment.ReadText(SingleOutputFileName));
            });
        }
        protected static void AssertSingleFileErrors(string input, Action<ImmutableArray<GeneratorError>> assertErrors) {
            AssertSingleFileResult(input, (result, testEnvironment) => {
                Assert.NotEmpty(result.Errors);
                Assert.All(result.Errors, error => Assert.Equal(SingleInputFileName, error.File));
                Assert.Equal(1, testEnvironment.FileCount);
                Assert.Empty(result.Files);
                assertErrors(result.Errors);
            });
        }
        protected static void AssertError(GeneratorError error, string id, string message, int lineNumber, int columnNumber) {
            Assert.Equal(id, error.Id);
            Assert.Equal(message, error.Message);
            Assert.Equal(lineNumber, error.LineNumber);
            Assert.Equal(columnNumber, error.ColumnNumber);
            Assert.Equal(lineNumber, error.EndLineNumber);
            Assert.Equal(columnNumber, error.EndColumnNumber);
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

