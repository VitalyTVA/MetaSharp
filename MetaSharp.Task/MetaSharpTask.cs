using Microsoft.Build.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace MetaSharp.Tasks {
    public class MetaSharpTask : ITask {
        public IBuildEngine BuildEngine { get; set; }
        public ITaskHost HostObject { get; set; }

        [Required]
        public ITaskItem[] InputFiles { get; set; }
        [Output]
        public ITaskItem[] OutputFiles { get; set; }

        public bool Execute() {
            for(int i = 0; i < InputFiles.Length; i++) {
                if(InputFiles[i].ItemSpec.EndsWith(".meta.cs")) {
                    var text = ProcessXyzFile(File.ReadAllText(InputFiles[i].ItemSpec), Path.GetFileName(InputFiles[i].ItemSpec));
                    File.WriteAllText(OutputFiles[i].ItemSpec, text);
                }
            }
            OutputFiles = OutputFiles.Where(x => x.ItemSpec.Contains(".meta")).ToArray();
            return true;
        }

        private string ProcessXyzFile(string xyzFileContents, string fileName) {
            return "namespace Gen { public class " + fileName.Replace('.', '_') + " { public const int Count = " + xyzFileContents.Split('\r').Length + "; } }";
        }
    }
}
