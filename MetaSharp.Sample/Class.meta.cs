using MetaSharp;
using System.Collections.Immutable;
using System.Collections.Generic;

[assembly: MetaInclude("helper.cs")]
[assembly: MetaReference(@"System.Collections.Immutable.dll", ReferenceRelativeLocation.TargetPath)]

namespace MetaSharp.Sample {
    using System;
    using System.Linq;
    public static class Class1 {
        [MetaLocation(MetaLocation.Project)]
        public static Either<IEnumerable<MetaError>, IEnumerable<Output>> CompletePOCOModels(MetaContext context) {
            return context.Complete(new[] { "Incomplete.cs" });
        }
        public static string Do(MetaContext context) {
            var name = ImmutableArray.Create("B").Single();
            return context.WrapMembers(
$@"public class {name} {{
    public static void Bla2() {{
    }}
}}");
        }
        [MetaLocation(MetaLocation.Project)]
        public static string Do2(MetaContext context) => Helper.Do2(context);

        public static string Do3(MetaContext context) {
            var classGenerator = ClassGenerator.Class<D3>()
                .Property<B>(x => x.Some2)
                .Property<int>(x => x.Some);
            return context.WrapMembers(classGenerator.Generate());
        }
    }
}
