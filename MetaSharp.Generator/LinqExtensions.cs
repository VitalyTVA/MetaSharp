using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MetaSharp.Utils {
    public static class LinqExtensions {
        public static ImmutableArray<T> YieldToImmutable<T>(this T item) {
            return ImmutableArray.Create(item);
        }
    }
}
