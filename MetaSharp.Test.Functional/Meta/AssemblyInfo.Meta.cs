using MetaSharp;
using System.Collections.Generic;

[assembly: MetaReference(@"..\..\Bin\System.Collections.Immutable.dll")]
[assembly: MetaReference(@"..\..\MetaSharp.Test\Bin\MetaSharp.Test.dll")]

namespace MetaSharp.Test.Meta {
    public static class CompleteFiles {
        [MetaLocation(MetaLocationKind.Designer)]
        public static Either<IEnumerable<MetaError>, Output> CompletePOCOModels(MetaContext context) {
            return context.Complete("POCOViewModels.cs")
                .Select(text => new Output(text, "POCOViewModels.designer.cs"));
        }
    }
}
