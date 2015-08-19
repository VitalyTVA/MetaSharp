using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
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
        public string IntermediatePath { get; set; }
        [Required]
        public ITaskItem[] InputFiles { get; set; }
        //[Output]
        //public ITaskItem[] OutputFiles { get; set; }

        public bool Execute() {
            var OutputFiles = InputFiles
                .Where(x => x.ItemSpec.EndsWith(".meta.cs"))
                .Select(x => new TaskItem(x) {
                    ItemSpec = Path.ChangeExtension(x.ItemSpec, ".designer.cs")
                })
                .ToArray();
            for(int i = 0; i < InputFiles.Length; i++) {
                if(InputFiles[i].ItemSpec.EndsWith(".meta.cs")) {
                    var text = ProcessXyzFile(File.ReadAllText(InputFiles[i].ItemSpec), Path.GetFileName(InputFiles[i].ItemSpec));
                    var oldText = File.ReadAllText(OutputFiles[i].ItemSpec);
                    if(oldText != text)
                        File.WriteAllText(OutputFiles[i].ItemSpec, text);
                }
            }
            return true;
        }

        private string ProcessXyzFile(string xyzFileContents, string fileName) {
            return "namespace Gen { public class " + fileName.Replace('.', '_') + " { public const int Count = " + xyzFileContents.Split('\r').Length + "; } }";
        }
    }
}
