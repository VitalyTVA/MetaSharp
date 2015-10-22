using MetaSharp;
using MetaSharp.Native;
using System.Collections.Generic;

[assembly: MetaReference(@"..\..\Bin\System.Collections.Immutable.dll")]
[assembly: MetaReference(@"..\..\packages\DevExpressMvvm.15.1.4.0\lib\net40-client\DevExpress.Mvvm.dll")]
[assembly: MetaReference(@"..\..\MetaSharp.Test\Bin\MetaSharp.Test.dll")]

namespace MetaSharp.Test.Meta {
    public static class CompleteFiles {
        public static Either<IEnumerable<MetaError>, IEnumerable<Output>> CompletePOCOModels(MetaContext context) {
            return context.Complete("POCOViewModels.cs".Yield(), new[] { new MetaCompleteViewModelAttribute() });
        }
    }
}

