using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace MetaSharp.Test {
    public class MetaContextTest {
        [Fact]
        public void WrapMembers() {
            var context = new MetaContext("Some.Namepace", new[] { "using System;", "using System.Linq;" });
            var input = 
@"public class B {

}";
            var result =
@"using System;
using System.Linq;

namespace Some.Namepace {
    public class B {

    }
}";
            Assert.Equal(result, context.WrapMembers(input)); 
        }
    }
}
