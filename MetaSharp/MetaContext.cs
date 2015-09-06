using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MetaSharp {
    public class MetaContext {
        public string Namespace { get; }
        public string[] Usings { get; }
        public MetaContext(string @namespace, string[] usings) {
            Namespace = @namespace;
            Usings = usings;
        }
    }
}
