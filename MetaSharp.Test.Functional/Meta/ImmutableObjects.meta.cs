using MetaSharp;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MetaSharp.Test.Meta {
    [MetaLocation(Location = MetaLocationKind.Designer)]
    static class ImmutableObjects {
        public static string Create(MetaContext context) {
            var name = ImmutableArray.Create("B").Single();
            return context.WrapMembers(
$@"public static class {name} {{
    public static void Bla2() {{
    }}
}}");
        }
    }
}
