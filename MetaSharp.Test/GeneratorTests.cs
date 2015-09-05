using MetaSharp.Tasks;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
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
            AssertSingleFileSimpleOutput(input, "Hello World!");
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
            AssertSingleFileSimpleOutput(input, string.Empty);
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
            AssertSingleFileSimpleOutput(input, "Hello World!");
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
            AssertSingleFileSimpleOutput(input, string.Empty);
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
            AssertSingleFileSimpleOutput(input, "Hello World!\r\nHello World Again!");
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
        [Fact]
        public void ConditionalSymbol() {
            var input = @"
#if METASHARP
namespace MetaSharp.HelloWorld {
    public static class HelloWorldGenerator {
        public static string SayHello() {
             return ""Hello World!"";
        }
    }
}
#endif
";
            AssertSingleFileSimpleOutput(input, "Hello World!");
        }
        [Fact]
        public void NonDefaultIntermediateOutputPathAndFileName() {
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
            var name = "file123.meta.cs";
            AssertMultipleFilesOutput(
                new TestFile(name, input).YieldToImmutable(),
                new TestFile(GetOutputFileName(name, "obf123"), output).YieldToImmutable(),
                "obf123"
            );
        }

        static void AssertSingleFileSimpleOutput(string input, string output) {
            AssertSingleFileOutput(input, GetFullSimpleOutput(output));
        }
        static string GetFullSimpleOutput(string output) {
            return output;
        }

    }
    public class TestFile {
        public readonly string Name, Text;
        public TestFile(string name, string text) {
            Name = name;
            Text = text;
        }
    }
    public class GeneratorTestsBase {
        const string DefaultIntermediateOutputPath = "obj";

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
        static void AssertMultipleFilesResult(ImmutableArray<TestFile> input, Action<GeneratorResult, TestEnvironment> assertion, string intermediateOutputPath) {
            var testEnvironment = CreateEnvironment(intermediateOutputPath);
            input.ForEach(file => testEnvironment.Environment.WriteText(file.Name, file.Text));
            var result = Generator.Generate(input.Select(file => file.Name).ToImmutableArray(), testEnvironment.Environment, PlatformEnvironment.DefaultReferences);
            AssertFiles(input, testEnvironment);
            assertion(result, testEnvironment);
        }
        protected static void AssertMultipleFilesOutput(ImmutableArray<TestFile> input, ImmutableArray<TestFile> output, string intermediateOutputPath = DefaultIntermediateOutputPath) {
            AssertMultipleFilesResult(input, (result, testEnvironment) => {
                Assert.Empty(result.Errors);
                Assert.Equal<string>(output.Select(x => x.Name).ToImmutableArray(), result.Files);
                Assert.Equal(input.Length + output.Length, testEnvironment.FileCount);
                AssertFiles(output, testEnvironment);
            }, intermediateOutputPath);
        }
        protected static void AssertMultipleFilesErrors(ImmutableArray<TestFile> input, Action<ImmutableArray<GeneratorError>> assertErrors, string intermediateOutputPath = DefaultIntermediateOutputPath) {
            AssertMultipleFilesResult(input, (result, testEnvironment) => {
                Assert.NotEmpty(result.Errors);
                Assert.Equal(input.Length, testEnvironment.FileCount);
                Assert.Empty(result.Files);
                assertErrors(result.Errors);
            }, intermediateOutputPath);
        }
        static void AssertFiles(ImmutableArray<TestFile> files, TestEnvironment environment) {
            files.ForEach(file => Assert.Equal(file.Text, environment.ReadText(file.Name)));
        }




        const string SingleInputFileName = "file.meta.cs";
        protected static void AssertSingleFileOutput(string input, string output) {
            AssertMultipleFilesOutput(
                new TestFile(SingleInputFileName, input).YieldToImmutable(),
                new TestFile(GetOutputFileName(SingleInputFileName), output).YieldToImmutable()
            );
        }
        protected static void AssertSingleFileErrors(string input, Action<ImmutableArray<GeneratorError>> assertErrors) {
            AssertMultipleFilesErrors(
                ImmutableArray.Create(new TestFile(SingleInputFileName, input)),
                errors => {
                    Assert.All(errors, error => Assert.Equal(SingleInputFileName, error.File));
                    assertErrors(errors);
                }
            );
        }
        protected static void AssertError(GeneratorError error, string id, string message, int lineNumber, int columnNumber) {
            Assert.Equal(id, error.Id);
            Assert.Equal(message, error.Message);
            Assert.Equal(lineNumber, error.LineNumber);
            Assert.Equal(columnNumber, error.ColumnNumber);
            Assert.Equal(lineNumber, error.EndLineNumber);
            Assert.Equal(columnNumber, error.EndColumnNumber);
        }

        protected static string GetOutputFileName(string input, string intermediateOutputPath = DefaultIntermediateOutputPath) {
            return Path.Combine(intermediateOutputPath, input.ReplaceEnd(".meta.cs", ".meta.g.i.cs"));
        }

        static TestEnvironment CreateEnvironment(string intermediateOutputPath) {
            var files = new Dictionary<string, string>();
            var environment = PlatformEnvironment.Create(
                fileName => files[fileName],
                (fileName, text) => files[fileName] = text,
                intermediateOutputPath
            );
            return new TestEnvironment(files, environment);
        }
    }
}

