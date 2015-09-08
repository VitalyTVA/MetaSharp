using MetaSharp;
using System.Collections.Immutable;
using System.Linq;

[assembly: MetaInclude("helper.cs")]
[assembly: MetaReference(@"..\packages\System.Collections.Immutable.1.1.36\lib\portable-net45+win8+wp8+wpa81\System.Collections.Immutable.dll")]

namespace MetaSharp.Sample {
    public static class Class1 {
        public static string Do() {
            System.Diagnostics.Debugger.Launch();
            var name = ImmutableArray.Create("B").Single();
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
        public static string Do2() => Helper.Do2();

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
