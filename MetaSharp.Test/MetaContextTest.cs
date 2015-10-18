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
            var context = new MetaContext("Some.Namepace", new[] { "using System;", "using System.Linq;" }, null);
            var input = 
@"public class B {

}";
            var input2 =
@"public class C {

}";
            var result =
@"namespace Some.Namepace {
using System;
using System.Linq;

    public class B {

    }
}";
            var result2 =
@"namespace Some.Namepace {
using System;
using System.Linq;

    public class B {

    }
    public class C {

    }
}";
            Assert.Equal(result, context.WrapMembers(input));
            Assert.Equal(result2, context.WrapMembers(new[] { input, input2 }));
        }
    }
}
