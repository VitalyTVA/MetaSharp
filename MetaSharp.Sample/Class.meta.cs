using MetaSharp;
using System.Collections.Immutable;

[assembly: MetaProto("Incomplete.cs", MetaLocationKind.Designer)]
[assembly: MetaInclude("helper.cs")]
[assembly: MetaReference(@"System.Collections.Immutable.dll", RelativeLocation.TargetPath)]

namespace MetaSharp.Sample {
    using System;
    using System.Linq;
    public static class Class1 {
        public static string Do(MetaContext context) {
            var name = ImmutableArray.Create("B").Single();
            return context.WrapMembers(
$@"public class {name} {{
    public static void Bla2() {{
    }}
}}");
        }
        [MetaLocation(MetaLocationKind.Designer)]
        public static string Do2(MetaContext context) => Helper.Do2(context);

        [MetaLocation(MetaLocationKind.IntermediateOutputNoIntellisense)]
        public static string Do3(MetaContext context) {
            var classGenerator = ClassGenerator.Class<D3>()
                .Property<B>(x => x.Some2)
                .Property<int>(x => x.Some);
            return context.WrapMembers(classGenerator.Generate());
        }
    }
}
