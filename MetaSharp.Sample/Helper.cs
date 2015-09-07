using System;

namespace MetaSharp.Sample {
    static class Helper {
        [MetaLocation(MetaLocationKind.Designer)]
        internal static string Do2() {
            var name = "D2";
            return $@"
namespace Gen {{
    public static class {name} {{
        public static void Bla2() {{
        }}
    }}
}}
";
        }
    }
}
