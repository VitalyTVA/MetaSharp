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
            //DoWork(args);
            //System.Windows.Forms.MessageBox.Show("");
            return DoWork(args);
        }

        private static int DoWork(string[] args) {
            var sw = new Stopwatch();
            sw.Start();
            try {
                return Process(args);
            } catch(Exception e) {
                System.Console.WriteLine(e.ToString());
                return 1;
            } finally {
                System.Console.WriteLine($"Done in {sw.ElapsedMilliseconds}ms");
            }
        }

        static int Process(string[] args) {
            var parser = GetParser();
            var result = parser.Parse(args);

            //Debugger.Launch();

            if(result.HasErrors) {
                System.Console.WriteLine(result.ErrorText);
                return (int)GeneratorResultCode.Error;
            }

            Directory.SetCurrentDirectory(parser.Object.ProjectPath);
            var files = Directory.GetFiles(Directory.GetCurrentDirectory(), "*." + Generator.DefaultInputFileEnd, SearchOption.AllDirectories)
                .ToImmutableArray();
            var buildConstants = new BuildConstants(
                intermediateOutputPath: null,
                targetPath: parser.Object.TargetPath,
                generatorMode: GeneratorMode.ConsoleApp
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
                .As('t', "targetPath")
                .SetDefault(string.Empty);
            p.Setup(arg => arg.ProjectPath)
                .As('p', "projectPath")
                .Required();
            return p;
        }
    }

    public class ApplicationArguments {
        public string TargetPath { get; set; }
        public string ProjectPath { get; set; }
    }
}
