using System;
using System.Collections.Generic;
//using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MetaSharp.Sample {
    public static class Class1 {


        public static string Do() {
            var name = "D";
            return $@"
namespace Gen {{
    public static class {name} {{

    }}
}}
";
        }
    }
}
