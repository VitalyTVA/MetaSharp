using MetaSharp.Test.Meta;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace MetaSharp.Test.Functional {
    public class GeneratedClassesTests {
        [Fact]
        public void SimpleImmutableObject() {
            B.Bla2();
        }
        [Fact]
        public void RemotelyGeneratedObject() {
            new RemotelyGeneratedClass();
        }
    }
}
