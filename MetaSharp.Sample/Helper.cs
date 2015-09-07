using System;

namespace MetaSharp.Sample {
    public static class Helper {
        [MetaLocation(MetaLocationKind.Designer)]
        public static string Do2() {
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
