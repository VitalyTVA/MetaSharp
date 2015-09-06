using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MetaSharp {
    public class GeneratorContext {
        public string Namespace { get; }
        public GeneratorContext(string @namespace) {
            Namespace = @namespace;
        }
    }
}
