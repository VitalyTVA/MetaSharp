using Fclp;
using MetaSharp.Native;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MetaSharp.Console {
    public static class Program {
        public static int Main(string[] args) {
            var parser = GetParser();
            var result = parser.Parse(args);

            //Debugger.Launch();

            if(result.HasErrors) {
                System.Console.WriteLine(result.ErrorText);
                return (int)GeneratorResultCode.Error;
            }

            var files = Directory.GetFiles(parser.Object.ProjectPath, "*." + Generator.DefaultInputFileEnd, SearchOption.AllDirectories)
                .Select(file => Path.GetFullPath(file))
                .ToImmutableArray();
            var buildConstants = new BuildConstants(
                intermediateOutputPath: null,
                targetPath: parser.Object.TargetPath
            );

            var code = RealEnvironmentGenerator.Generate(
                files,
                buildConstants,
                error => System.Console.WriteLine(error.ToString()),
                output => { }
            );

            return (int)code;
        }

        static FluentCommandLineParser<ApplicationArguments> GetParser() {
            var p = new FluentCommandLineParser<ApplicationArguments>();
            p.Setup(arg => arg.TargetPath)
                .As("targetPath")
                .SetDefault(string.Empty);
            p.Setup(arg => arg.ProjectPath)
                .As("projectPath")
                .Required();
            return p;
        }
    }

    public class ApplicationArguments {
        public string TargetPath { get; set; }
        public string ProjectPath { get; set; }
    }
}
