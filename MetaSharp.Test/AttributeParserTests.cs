using MetaSharp.Native;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace MetaSharp.Test {
    public class AttrubteParserTests {
        public class BindableProperty {
            public readonly bool IsBindable;
            public BindableProperty() {
            }
            public BindableProperty(bool isBindable) {
                IsBindable = isBindable;
            }
            public override bool Equals(object obj) {
                var other = obj as BindableProperty;
                return other != null && other.IsBindable == IsBindable;
            }
            public override int GetHashCode() {
                throw new NotSupportedException();
            }
        }
        [Fact]
        public void ParseSimple() {
            AssertParseAttributes("[Bindable(true)]", new BindableProperty());
            AssertParseAttributes("[BindableAttribute]", new BindableProperty());
            AssertParseAttributes("[Bindable_]", default(BindableProperty));
        }

        void AssertParseAttributes<T>(string attributes, T expected) where T : class {
            var code = $@"
public class TestClass {{
    {attributes}
    public string TestProperty {{ get; set; }}
}}
";
            var compilation = CSharpCompilation.Create("test", SyntaxFactory.ParseSyntaxTree(code).Yield());
            var property = compilation.GetTypeByMetadataName("TestClass").Properties().Single();
            var parsed = AttributeParser.Parse<T>(property);
            Assert.Equal(expected, parsed);
        }
    }
}