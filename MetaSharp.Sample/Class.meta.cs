
namespace MetaSharp.Sample {
    public static class Class1 {
        public static string Do() {
            var name = "D";
            return $@"
namespace Gen {{
    public static class {name} {{
        public static void Bla2() {{
        }}
    }}
}}
";
        }
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
        [MetaLocation(MetaLocationKind.IntermediateOutputNoIntellisense)]
        public static string Do3() {
            var name = "D3";
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
