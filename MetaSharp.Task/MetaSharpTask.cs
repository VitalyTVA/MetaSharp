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
                if(InputFiles[i].ItemSpec.EndsWith(".meta.cs"))
                    File.WriteAllText(OutputFiles[i].ItemSpec, ProcessXyzFile(File.ReadAllText(InputFiles[i].ItemSpec)));
            }
            return true;
        }

        private string ProcessXyzFile(string xyzFileContents) {
            return "/*---" + xyzFileContents + "*/";
        }
    }
}
