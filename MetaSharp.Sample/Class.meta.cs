using MetaSharp;
using System.Collections.Immutable;

[assembly: MetaInclude("helper.cs")]
[assembly: MetaReference(@"System.Collections.Immutable.dll", RelativeLocation.TargetPath)]

namespace MetaSharp.Sample {
    using System.Linq;
    public static class Class1 {
        public static string Do(MetaContext context) {
            var name = ImmutableArray.Create("B").Single();
            return context.WrapMembers(
$@"public static class {name} {{
    public static void Bla2() {{
    }}
}}");
        }
        [MetaLocation(MetaLocationKind.Designer)]
        public static string Do2(MetaContext context) => Helper.Do2(context);

        [MetaLocation(MetaLocationKind.IntermediateOutputNoIntellisense)]
        public static string Do3(MetaContext context) {
            return context.WrapMembers(ClassGenerator.Class<D3>().Generate());
        }
    }
}
