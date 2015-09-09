using System;

namespace MetaSharp.Sample {
    public static class Helper {
        [MetaLocation(MetaLocationKind.Designer)]
        public static string Do2(MetaContext context) {
            var name = "D2";
            return context.WrapMembers($@"
public static class {name} {{
    public static void Bla2() {{
    }}
}}");
        }
    }
}
