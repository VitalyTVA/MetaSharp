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

        //TODO make Combine methods auto-generated (self hosting)
        public static Either<ImmutableArray<TLeft>, TResult> Combine<TLeft, T1, T2, T3, TResult>(
            Either<TLeft, T1> x1,
            Either<TLeft, T2> x2,
            Either<TLeft, T3> x3,
            Func<T1, T2, T3, TResult> combine
        ) {
            var lefts = Lefts(x1, x2, x3).ToImmutableArray();
            if(lefts.Any())
                return lefts;
            return combine(x1.ToRight(), x2.ToRight(), x3.ToRight());
        }
        static IEnumerable<TLeft> Lefts<TLeft, T1, T2, T3>(
            Either<TLeft, T1> x1,
            Either<TLeft, T2> x2,
            Either<TLeft, T3> x3) {
            if(x1.IsLeft())
                yield return x1.ToLeft();
            if(x2.IsLeft())
                yield return x2.ToLeft();
            if(x3.IsLeft())
                yield return x3.ToLeft();
        }
    }
}
