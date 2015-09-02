using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace MetaSharp {
    public static class Generator {
        public static void Generate(ImmutableArray<string> files, FileSystem fileSystem) {
            //List<SyntaxTree> trees = new List<SyntaxTree>();
            //List<string> files = new List<string>();
            //for(int i = 0; i < InputFiles.Length; i++) {
            //    if(InputFiles[i].ItemSpec.EndsWith(".meta.cs")) {
            //        trees.Add(SyntaxFactory.ParseSyntaxTree(File.ReadAllText(InputFiles[i].ItemSpec)));
            //        files.Add(InputFiles[i].ItemSpec);
            //    }

            //}
        }
    }
    public class FileSystem {
        public readonly Func<string, string> ReadText;
        public readonly Func<string, string> WriteText;
        public FileSystem(Func<string, string> readText, Func<string, string> writeText) {
            ReadText = readText;
            WriteText = writeText;
        }
    }
}
