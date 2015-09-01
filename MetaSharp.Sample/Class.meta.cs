
namespace MetaSharp.Sample {
    public static class Class1 {
        public static string Do() {
            var name = "D";
            return $@"
namespace Gen {{
    public static class {name} {{
        public static void Bla() {{
        }}
    }}
}}
";
        }
    }
}
