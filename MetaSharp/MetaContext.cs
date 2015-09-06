using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MetaSharp {
    public class MetaContext {
        public string Namespace { get; }
        public IEnumerable<string> Usings { get; }
        public MetaContext(string @namespace, IEnumerable<string> usings) {
            Namespace = @namespace;
            Usings = usings;
        }
    }
    public enum MetaLocationKind {
        IntermediateOutput,
        IntermediateOutputNoIntellisense,
        Designer,
    }
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class MetaLocationAttribute : Attribute {
        public MetaLocationAttribute(MetaLocationKind location = default(MetaLocationKind)) {
            Location = location;
        }
        public MetaLocationKind Location { get; set; }
    }
}
