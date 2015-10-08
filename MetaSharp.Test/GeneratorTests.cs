using MetaSharp.Tasks;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using Xunit;
using MetaSharp.Utils;
using MetaSharp.Native;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;

namespace MetaSharp.Test {
    public class  HelloWorldTests : GeneratorTestsBase {
        [Fact]
        public void IsMetaSharpFile() {
            Assert.True(Generator.IsMetaSharpFile("file.meta.cs"));
            Assert.False(Generator.IsMetaSharpFile("file.teta.cs"));
            Assert.False(Generator.IsMetaSharpFile("file.meta.cs.cs"));
        }
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
        public void NonPublicMethods() {
            var input = @"
namespace MetaSharp.HelloWorld.NonPublicMethod {
    static class HelloWorldGenerator_NonPublicMethod {
        public static string SayHello() {
             return ""Hello World!"";
        }
        internal static string SayHelloInternal() {
             return ""Hello World!"";
        }
        static string SayHelloPrivatly() {
            throw new System.NotImplementedException();
        }
    }
}
";
            AssertSingleFileSimpleOutput(input, "Hello World!");
        }
        [Fact]
        public void NoMethods() {
            var input = @"
namespace MetaSharp.HelloWorld.NonPublicMethod {
    static class HelloWorldGenerator_NonPublicMethod {
        internal static string Hello => ""Hello"";
    }
}
";
            AssertMultipleFilesOutput(
                new TestFile(SingleInputFileName, input).YieldToImmutable(),
                ImmutableArray<TestFile>.Empty
            );
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
        public static string GenericFail<T>() {
             throw new NotImplementedException();
        }
        public static string CannotSayHelloWithWrongParameters(int some) {
             throw new NotImplementedException();
        }
    }
}
";
            AssertSingleFileSimpleOutput(input, "Hello World!\r\nHello World Again!");
        }
        [Fact]
        public void SeveralOutputsFromSingleMethod() {
            var input = @"
using System;
using System.Collections.Generic;
namespace MetaSharp.HelloWorld {
    public static class HelloWorldGenerator {
        public static IEnumerable<string> SayHelloManyTimes() {
            yield return ""Hello World!"";
            yield return ""Hello World Again!"";
        }
        public static string[] SayHelloManyTimesFromArray() {
            return new[] { 
                ""Hello World from array!"",
                ""Hello World from array again!"",
            };
        }
    }
}
";
            AssertSingleFileSimpleOutput(input, "Hello World!\r\nHello World Again!\r\nHello World from array!\r\nHello World from array again!");
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
                    error => {
                        AssertError(error, SingleInputFileName, "CS1002", "; expected", 5, 35);
                        Assert.Equal("file.meta.cs(5,35,5,35): error CS1002: ; expected", error.ToString());
                    },
                    error => AssertError(error, SingleInputFileName, "CS1513", "} expected", 8, 2));
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
            var name = "meta.cs.meta.cs";
            var path = "obf123";
            AssertMultipleFilesOutput(
                new TestFile(name, input).YieldToImmutable(),
                new TestFile(GetOutputFileName(name, path), "Hello World!").YieldToImmutable(),
                CreateBuildConstants(intermediateOutputPath: path)
            );
        }
        [Fact]
        public void MultipleFileErrors() {
            var input1 = @"
namespace MetaSharp.HelloWorld {
    public static class HelloWorldGenerator {
        public static string SayHello() {
             return ""Hello World!""
        }
    }
}
";
            var input2 = @"
namespace MetaSharp.HelloWorld {
    public static class HelloAgainGenerator {
        public static string SayHelloAgain() {
             return ""Hello Again!"";
        }
    }

";
            var name1 = "file1.meta.cs";
            var name2 = "file2.meta.cs";
            AssertMultipleFilesErrors(
                ImmutableArray.Create(new TestFile(name1, input1), new TestFile(name2, input2)),
                errors => {
                    Assert.Collection(errors,
                        error => AssertError(error, name1, "CS1002"),
                        error => AssertError(error, name2, "CS1513"));
                }
            );
        }
        [Fact]
        public void MultipleFileOutput() {
            var input1 = @"
namespace MetaSharp.HelloWorld {
    public static class HelloWorldGenerator {
        public static string SayHello() {
             return ""Hello World!"";
        }
    }
}
";
            var input2 = @"
namespace MetaSharp.HelloAgain {
    public static class HelloWorldGenerator {
        public static string SayHelloAgain() {
             return ""Hello Again!"";
        }
    }
}
";
            var name1 = "file1.meta.cs";
            var name2 = "file2.meta.cs";
            AssertMultipleFilesOutput(
                ImmutableArray.Create(new TestFile(name2, input2), new TestFile(name1, input1)),
                ImmutableArray.Create(
                    new TestFile(GetOutputFileName(name1), "Hello World!"), 
                    new TestFile(GetOutputFileName(name2), "Hello Again!")
                )
            );
        }
        [Fact]
        public void PartialDefinitions() {
            var input1 = @"
namespace MetaSharp.HelloWorld {
    public static partial class HelloWorldGenerator {
        public static string SayHello() {
             return ""Hello World!"";
        }
    }
}
";
            var input2 = @"
namespace MetaSharp.HelloWorld {
    partial class HelloWorldGenerator {
        public static string SayHelloAgain() {
             return ""Hello Again!"";
        }
    }
}
";
            var name1 = "file1.meta.cs";
            var name2 = "file2.meta.cs";
            AssertMultipleFilesOutput(
                ImmutableArray.Create(new TestFile(name2, input2), new TestFile(name1, input1)),
                ImmutableArray.Create(
                    new TestFile(GetOutputFileName(name1), "Hello World!"),
                    new TestFile(GetOutputFileName(name2), "Hello Again!")
                )
            );
        }
        [Fact]
        public void UseMetaContext() {
            var input = @"
using MetaSharp;
using System.Collections;
namespace MetaSharp.HelloWorld {
using System;
using System.Linq;
using Alias = System.Action;
    public static class HelloWorldGenerator {
        public static string SayHello(MetaContext context) {
             return context.Usings.Count() + string.Concat(context.Usings) + ""Hello World from "" + context.Namespace;
        }
        public static string CannotSayHelloWithOutParameter(out MetaContext context) {
            throw new NotImplementedException();
        }
        public static string CannotSayHelloWithRefParameter(ref MetaContext context) {
            throw new NotImplementedException();
        }
    }
}
";
            const string output =
                "3using System;using System.Linq;using Alias = System.Action;" +
                "Hello World from MetaSharp.HelloWorld";
            AssertSingleFileSimpleOutput(input, output);
        }
        [Fact]
        public void SeveralOutputLocationKindsForTypes() {
            var input = @"
using MetaSharp;
namespace MetaSharp.HelloWorld {
    public static class HelloWorldGenerator {
        public static string SayHello() {
             return ""Hello World!"";
        }
    }
    [MetaLocation(MetaLocationKind.IntermediateOutputNoIntellisense)]
    public static class HelloWorldGenerator_NoIntellisense {
        public static string SayHelloAgain() {
             return ""I am hidden!"";
        }
    }
    [MetaLocation(Location = MetaLocationKind.Designer)]
    public static class HelloWorldGenerator_Designer{
        public static string SayHelloAgain() {
             return ""I am dependent upon!"";
        }
    }
}
";
            var name = "file.meta.cs";
            AssertMultipleFilesOutput(
                new TestFile(name, input).YieldToImmutable(),
                ImmutableArray.Create(
                    new TestFile(GetOutputFileName(name), "Hello World!"),
                    new TestFile(GetOutputFileNameNoIntellisense(name), "I am hidden!"),
                    new TestFile(GetOutputFileNameDesigner(name), "I am dependent upon!", isInFlow: false)
                )
            );
        }
        [Fact]
        public void SeveralOutputLocationKindsForMethods() {
            var input = @"
using MetaSharp;
namespace MetaSharp.HelloWorld {
    [MetaLocation(Location = MetaLocationKind.Designer)]
    public static class HelloWorldGenerator {
        [MetaLocation(MetaLocationKind.IntermediateOutput)]
        public static string SayHello() {
             return ""Hello World!"";
        }
        [MetaLocation(MetaLocationKind.IntermediateOutputNoIntellisense)]
        public static string SayHelloAgain() {
             return ""I am hidden!"";
        }
        [MetaLocation(Location = MetaLocationKind.Designer)]
        public static string SayHelloOneMoreTime() {
             return ""I am dependent upon!"";
        }
    }
}
";
            var name = "file.meta.cs";
            AssertMultipleFilesOutput(
                new TestFile(name, input).YieldToImmutable(),
                ImmutableArray.Create(
                    new TestFile(GetOutputFileName(name), "Hello World!"),
                    new TestFile(GetOutputFileNameNoIntellisense(name), "I am hidden!"),
                    new TestFile(GetOutputFileNameDesigner(name), "I am dependent upon!", isInFlow: false)
                )
            );
        }
        [Fact]
        public void Include() {
            var include1 = @"
namespace MetaSharp.HelloWorld {
    public class Helper { 
        public static string SayHello() {
             return ""Hello World!"";
        }
    }
}
";
            var include2 = @"
namespace MetaSharp.HelloWorld {
    public static class Helper2 { 
        public static string SayHelloAgain() {
             return ""Hello Again!"";
        }
    }
}
";
            var input = @"
using MetaSharp;
[assembly: MetaInclude(MetaSharp.HelloWorld.HelloWorldGenerator.Include)]
[assembly: MetaInclude(""Helper2.cs"")]
[assembly: System.CLSCompliant(false)]
namespace MetaSharp.HelloWorld {
    public static class HelloWorldGenerator {
        public const string Include = ""SubDir\\Helper.cs"";
        public static string SayHello() => Helper.SayHello() + Helper2.SayHelloAgain();
    }
}
";
            var fileName = SingleInputFileName;
            AssertMultipleFilesOutput(
                ImmutableArray.Create(
                    new TestFile(fileName, input),
                    new TestFile("SubDir\\Helper.cs", include1, isInFlow: false),
                    new TestFile("Helper2.cs", include2, isInFlow: false)
                ),
                new TestFile(GetOutputFileName(fileName), "Hello World!Hello Again!").YieldToImmutable());
        }
        [Fact]
        public void Reference() {
            var input = @"
using MetaSharp;
using Xunit;
using System.Linq;
using System.Collections.Immutable;
using System.Windows.Media;
using System.Windows.Forms;
using System.Drawing;

[assembly: MetaReference(""System.Collections.Immutable.dll"")]
[assembly: MetaReference(""bin\\Xunit.Assert.dll"", ReferenceRelativeLocation.TargetPath)]
[assembly: MetaReference(""WPF\\WindowsBase.dll"", ReferenceRelativeLocation.Framework)]
[assembly: MetaReference(""WPF\\PresentationCore.dll"", ReferenceRelativeLocation.Framework)]
[assembly: MetaReference(""System.Drawing.dll"", ReferenceRelativeLocation.Framework)]
[assembly: MetaReference(""System.Windows.Forms.dll"", ReferenceRelativeLocation.Framework)]
namespace MetaSharp.HelloWorld {
    public static class HelloWorldGenerator {
        public static string SayHello(MetaContext context) {
            new SolidColorBrush();
            new PointF();
            Assert.Equal(""MetaSharp.HelloWorld"", context.Namespace);
            return ImmutableArray.Create(""Hello World!"").Single();
        }
    }
}
";
            AssertSingleFileOutput(input, GetFullSimpleOutput("Hello World!"), CreateBuildConstants(targetPath: ".."));
        }
    }
    public class TestFile {
        public readonly string Name, Text;
        public readonly bool IsInFlow;
        public TestFile(string name, string text, bool isInFlow = true) {
            Name = name;
            Text = text;
            IsInFlow = isInFlow;
        }
    }
    public class GeneratorTestsBase {
        const string DefaultIntermediateOutputPath = "obj";
        const string DefaultTargetPath = "bin";
        protected const string SingleInputFileName = "file.meta.cs";

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
        static void AssertMultipleFilesResult(ImmutableArray<TestFile> input, Action<GeneratorResult, TestEnvironment> assertion, BuildConstants buildConstants) {
            var testEnvironment = CreateEnvironment(buildConstants);
            input.ForEach(file => testEnvironment.Environment.WriteText(file.Name, file.Text));
            var result = Generator.Generate(input.Where(file => file.IsInFlow).Select(file => file.Name).ToImmutableArray(), testEnvironment.Environment);
            AssertFiles(input, testEnvironment, ignoreEmptyLines: false);
            assertion(result, testEnvironment);
        }
        protected static void AssertMultipleFilesOutput(ImmutableArray<TestFile> input, ImmutableArray<TestFile> output, BuildConstants buildConstants = null, bool ignoreEmptyLines = false) {
            AssertMultipleFilesResult(input, (result, testEnvironment) => {
                Assert.Empty(result.Errors);
                Assert.Equal<string>(
                    output
                        .Where(x => x.IsInFlow)
                        .Select(x => x.Name)
                        .OrderBy(x => x), 
                    result.Files.OrderBy(x => x));
                Assert.Equal(input.Length + output.Length, testEnvironment.FileCount);
                AssertFiles(output, testEnvironment, ignoreEmptyLines);
            }, buildConstants);
        }
        protected static void AssertMultipleFilesErrors(ImmutableArray<TestFile> input, Action<IEnumerable<GeneratorError>> assertErrors, BuildConstants buildConstants = null) {
            AssertMultipleFilesResult(input, (result, testEnvironment) => {
                Assert.NotEmpty(result.Errors);
                Assert.Equal(input.Length, testEnvironment.FileCount);
                Assert.Empty(result.Files);
                assertErrors(result.Errors.OrderBy(x => x.File));
            }, buildConstants);
        }
        static void AssertFiles(ImmutableArray<TestFile> files, TestEnvironment environment, bool ignoreEmptyLines) {
            files.ForEach(file => {
                var actualText = environment.ReadText(file.Name);
                if(ignoreEmptyLines)
                    actualText = actualText.RemoveEmptyLines();
                Assert.Equal(file.Text, actualText);
            });
        }

        protected static void AssertSingleFileSimpleOutput(string input, string output) {
            AssertSingleFileOutput(input, GetFullSimpleOutput(output));
        }
        protected static string GetFullSimpleOutput(string output)
            => output;
        protected static void AssertSingleFileOutput(string input, string output, BuildConstants buildConstants = null, bool ignoreEmptyLines = false) {
            AssertMultipleFilesOutput(
                new TestFile(SingleInputFileName, input).YieldToImmutable(),
                new TestFile(GetOutputFileName(SingleInputFileName), output).YieldToImmutable(),
                buildConstants,
                ignoreEmptyLines
            );
        }
        protected static void AssertSingleFileErrors(string input, Action<IEnumerable<GeneratorError>> assertErrors) {
            AssertMultipleFilesErrors(
                ImmutableArray.Create(new TestFile(SingleInputFileName, input)),
                errors => {
                    assertErrors(errors);
                }
            );
        }
        protected static void AssertError(GeneratorError error, string file, string id, string message, int lineNumber, int columnNumber) {
            AssertError(error, file, id);
            Assert.Equal(lineNumber, error.LineNumber);
            Assert.Equal(columnNumber, error.ColumnNumber);
            Assert.Equal(lineNumber, error.EndLineNumber);
            Assert.Equal(columnNumber, error.EndColumnNumber);
        }
        protected static void AssertError(GeneratorError error, string file, string id) {
            Assert.Equal(file, error.File);
            Assert.Equal(id, error.Id);
        }

        protected static string GetProtoOutputFileName(string input, string intermediateOutputPath = DefaultIntermediateOutputPath)
            => Path.Combine(intermediateOutputPath, input.ReplaceEnd(".cs", ".g.i.cs"));

        protected static string GetProtoOutputFileNameDesigner(string input, string intermediateOutputPath = DefaultIntermediateOutputPath)
            => input.ReplaceEnd(".cs", ".designer.cs");

        protected static string GetOutputFileName(string input, string intermediateOutputPath = DefaultIntermediateOutputPath)
            => GetOutputFileNameCore(input, intermediateOutputPath, "g.i.cs");

        protected static string GetOutputFileNameNoIntellisense(string input, string intermediateOutputPath = DefaultIntermediateOutputPath)
            => GetOutputFileNameCore(input, intermediateOutputPath, "g.cs");

        protected static string GetOutputFileNameDesigner(string input)
            => GetOutputFileNameCore(input, string.Empty, "designer.cs");

        protected static BuildConstants CreateBuildConstants(string intermediateOutputPath = DefaultIntermediateOutputPath, string targetPath = DefaultTargetPath)
            => new BuildConstants(
                intermediateOutputPath: intermediateOutputPath, 
                targetPath: targetPath
            );

        static string GetOutputFileNameCore(string input, string intermediateOutputPath, string suffix)
            => Path.Combine(intermediateOutputPath, input.ReplaceEnd(".meta.cs", ".meta." + suffix));

        static TestEnvironment CreateEnvironment(BuildConstants buildConstants) {
            var files = new Dictionary<string, string>();
            var environment = new Environment(
                readText: fileName => files[fileName],
                writeText: (fileName, text) => files[fileName] = text,
                buildConstants: buildConstants ?? CreateBuildConstants()
            );
            return new TestEnvironment(files, environment);
        }

        protected static void AssertCompiles(params string[] files) {
            var trees = files.Select(x => SyntaxFactory.ParseSyntaxTree(x));
            var compilation = CSharpCompilation.Create(
                "temp",
                references: Generator.DefaultReferences,
                options: new CSharpCompilationOptions(
                    OutputKind.DynamicallyLinkedLibrary,
                    assemblyIdentityComparer: DesktopAssemblyIdentityComparer.Default
                ),
                syntaxTrees: files.Select(x => SyntaxFactory.ParseSyntaxTree(x))
            );
            var errors = compilation.GetDiagnostics().Where(x => x.Severity == DiagnosticSeverity.Error);
            Assert.Empty(errors);
        }
    }
}

