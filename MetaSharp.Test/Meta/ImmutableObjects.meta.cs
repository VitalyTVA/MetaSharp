using MetaSharp;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

[assembly: MetaReference("System.Collections.Immutable.dll", RelativeLocation.TargetPath)]

namespace MetaSharp.Test.Meta {
    [MetaLocation(MetaLocationKind.Designer)]
    static class ImmutableObjects {
        public static string Do(MetaContext context) {
            var name = ImmutableArray.Create("B").Single();
            return context.WrapMembers(
$@"public static class {name} {{
    public static void Bla2() {{
    }}
}}");
        }
    }
}
