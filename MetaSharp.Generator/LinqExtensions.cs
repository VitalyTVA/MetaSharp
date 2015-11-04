using MetaSharp.Native;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MetaSharp.Utils {
    public static class LinqExtensionsEx {
        public static ImmutableArray<T> YieldToImmutable<T>(this T item) {
            return ImmutableArray.Create(item);
        }
        public static Either<TLeftNew, TRightNew> AggregateEither<TLeft, TRight, TLeftNew, TRightNew>(
            this IEnumerable<Either<TLeft, TRight>> source, 
            Func<IEnumerable<TLeft>, TLeftNew> selectorLeft, 
            Func<IEnumerable<TRight>, TRightNew> selectorRight) {
            var aggregated = source.AggregateEither(
                ImmutableStack<TLeft>.Empty,
                ImmutableStack<TRight>.Empty,
                (acc, value) => acc.Push(value),
                (acc, value) => acc.Push(value)
            );
            return aggregated.Transform(left => selectorLeft(left.Reverse()), right => selectorRight(right.Reverse()));
        }
    }
}
