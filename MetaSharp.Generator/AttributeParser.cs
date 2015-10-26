using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MetaSharp {
    public static class AttributeParser {
        public static T Parse<T>(IPropertySymbol property) where T : class {
            var attrubutes = ((PropertyDeclarationSyntax)property.Node())
                .AttributeLists
                .SelectMany(x => x.Attributes)
                .Where(x => x.Name.ToString() == typeof(T).Name || x.Name.ToString() == typeof(T).Name + "Attribute");
            if(!attrubutes.Any())
                return null;
            return Activator.CreateInstance(typeof(T)) as T;
        }
    }
}
