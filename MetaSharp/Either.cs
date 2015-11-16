using MetaSharp.Native;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MetaSharp {
    public abstract class Either<TLeft, TRight> {
        public static implicit operator Either<TLeft, TRight>(TLeft val)  {
            return new LeftValue(val);
        }
        public static implicit operator Either<TLeft, TRight>(TRight val) {
            return new RightValue(val);
        }
        public static Either<TLeft, TRight> Left(TLeft value) {
            return new LeftValue(value);
        }
        public static Either<TLeft, TRight> Right(TRight value) {
            return new RightValue(value);
        }
        Either() { }
        internal class LeftValue : Either<TLeft, TRight> {
            internal readonly TLeft Value;
            internal LeftValue(TLeft value) {
                Value = value;
            }
        }
        internal class RightValue : Either<TLeft, TRight> {
            internal readonly TRight Value;
            internal RightValue(TRight value) {
                Value = value;
            }
        }
    }
    public static class Either {
        //TODO use friend access diagnostics - only this class can access Either's internals
        public static bool IsRight<TLeft, TRight>(this Either<TLeft, TRight> value) {
            return value.Match(left => false, right => true);
        }
        public static bool IsLeft<TLeft, TRight>(this Either<TLeft, TRight> value) {
            return value.Match(left => true, right => false);
        }
        public static TLeft ToLeft<TLeft, TRight>(this Either<TLeft, TRight> value) {
            return value.Match(left => left, right => { throw new InvalidOperationException(); });
        }
        public static TRight ToRight<TLeft, TRight>(this Either<TLeft, TRight> value) {
            return value.Match(left => { throw new InvalidOperationException(); }, right => right);
        }
        public static T Match<TLeft, TRight, T>(this Either<TLeft, TRight> value, Func<TLeft, T> left, Func<TRight, T> right) {
            var leftValue = value as Either<TLeft, TRight>.LeftValue;
            if(leftValue != null)
                return left(leftValue.Value);
            return right((value as Either<TLeft, TRight>.RightValue).Value);
        }
        public static void Match<TLeft, TRight>(this Either<TLeft, TRight> value, Action<TLeft> left, Action<TRight> right) {
            var leftValue = value as Either<TLeft, TRight>.LeftValue;
            if(leftValue != null)
                left(leftValue.Value);
            right((value as Either<TLeft, TRight>.RightValue).Value);
        }
        public static Either<TLeft, TRightNew> Select<TLeft, TRight, TRightNew>(this Either<TLeft, TRight> value, Func<TRight, TRightNew> selector) {
            return value.Match(
                left => Either<TLeft, TRightNew>.Left(left),
                right => Either<TLeft, TRightNew>.Right(selector(right))
            );
        }
        public static Either<TLeftNew, TRight> SelectError<TLeft, TRight, TLeftNew>(this Either<TLeft, TRight> value, Func<TLeft, TLeftNew> selector) {
            return value.Match(
                left => Either<TLeftNew, TRight>.Left(selector(left)),
                right => Either<TLeftNew, TRight>.Right(right)
            );
        }
        public static Either<TLeft, TProjection> SelectMany<TLeft, TRight, TRightNew, TProjection>(
            //TODO duplicated code
            this Either<TLeft, TRight> value,
            Func<TRight, Either<TLeft, TRightNew>> selector,
            Func<TRight, TRightNew, TProjection> projector) {

            if(value.IsLeft())
                return Either<TLeft, TProjection>.Left(value.ToLeft());

            var res = selector(value.ToRight());
            if(res.IsLeft())
                return Either<TLeft, TProjection>.Left(res.ToLeft());

            return Either<TLeft, TProjection>.Right(projector(value.ToRight(), res.ToRight()));
        }
        public static Either<TLeft, TRightNew> SelectMany<TLeft, TRight, TRightNew>(
            //TODO duplicated code
            this Either<TLeft, TRight> value,
            Func<TRight, Either<TLeft, TRightNew>> selector) {

            if(value.IsLeft())
                return Either<TLeft, TRightNew>.Left(value.ToLeft());

            return selector(value.ToRight());
        }

        //public static Either<TLeft, TRight> Where<TLeft, TRight>(this Either<TLeft, TRight> value, Predicate<TRight> predicate) {
        //    return value.Match(
        //        left => Either<TLeft, TRightNew>.Left(left),
        //        right => Either<TLeft, TRightNew>.Right(selector(right))
        //    );
        //}
        public static Either<TLeftNew, TRightNew> Transform<TLeft, TRight, TLeftNew, TRightNew>(this Either<TLeft, TRight> value, Func<TLeft, TLeftNew> selectorLeft, Func<TRight, TRightNew> selectorRight) {
            return value.Match(
                left => Either<TLeftNew, TRightNew>.Left(selectorLeft(left)),
                right => Either<TLeftNew, TRightNew>.Right(selectorRight(right))
            );
        }
        public static Either<TLeftAcc, TRightAcc> AggregateEither<TLeft, TRight, TLeftAcc, TRightAcc>(
            this IEnumerable<Either<TLeft, TRight>> source,
            TLeftAcc leftSeed,
            TRightAcc rightSeed,
            Func<TLeftAcc, TLeft, TLeftAcc> leftAcc,
            Func<TRightAcc, TRight, TRightAcc> rightAcc) {
            return source.Aggregate(
                Either<TLeftAcc, TRightAcc>.Right(rightSeed),
                (acc, value) => acc.Match(
                    accLeft => Either<TLeftAcc, TRightAcc>.Left(value.Match(left => leftAcc(accLeft, left), right => accLeft)),
                    accRight => value.Match(left => Either<TLeftAcc, TRightAcc>.Left(leftAcc(leftSeed, left)), right => Either<TLeftAcc, TRightAcc>.Right(rightAcc(accRight, right)))
                )
            );
        }

        public static IEnumerable<Either<TLeft, TRight>> WhereEither<TLeft, TRight>(this IEnumerable<Either<TLeft, TRight>> source, Predicate<TRight> filter) {
            return source.Where(x => x.Match(left => true, right => filter(right)));
        }
        #region combine
        //TODO make Combine methods auto-generated (self hosting)
        //public static Either<IEnumerable<TLeft>, TResult> Combine<TLeft, T1, T2, TResult>(
        //    Either<TLeft, T1> x1,
        //    Either<TLeft, T2> x2,
        //    Func<T1, T2, TResult> combine
        //) {
        //    IEnumerable<TLeft> lefts = Lefts(x1, x2);
        //    if(lefts.Any())
        //        return Either<IEnumerable<TLeft>, TResult>.Left(lefts);
        //    return combine(x1.ToRight(), x2.ToRight());
        //}
        //static IEnumerable<TLeft> Lefts<TLeft, T1, T2>(
        //    Either<TLeft, T1> x1,
        //    Either<TLeft, T2> x2) {
        //    if(x1.IsLeft())
        //        yield return x1.ToLeft();
        //    if(x2.IsLeft())
        //        yield return x2.ToLeft();
        //}

        public static Either<IEnumerable<TLeft>, TResult> Combine<TLeft, T1, T2, T3, T4, TResult>(
            Either<TLeft, T1> x1,
            Either<TLeft, T2> x2,
            Either<TLeft, T3> x3,
            Either<TLeft, T4> x4,
            Func<T1, T2, T3, T4, TResult> combine
        ) {
            IEnumerable<TLeft> lefts = Lefts(x1, x2, x3, x4);
            if(lefts.Any())
                return Either<IEnumerable<TLeft>, TResult>.Left(lefts);
            return combine(x1.ToRight(), x2.ToRight(), x3.ToRight(), x4.ToRight());
        }
        static IEnumerable<TLeft> Lefts<TLeft, T1, T2, T3, T4>(
            Either<TLeft, T1> x1,
            Either<TLeft, T2> x2,
            Either<TLeft, T3> x3,
            Either<TLeft, T4> x4) {
            if(x1.IsLeft())
                yield return x1.ToLeft();
            if(x2.IsLeft())
                yield return x2.ToLeft();
            if(x3.IsLeft())
                yield return x3.ToLeft();
            if(x4.IsLeft())
                yield return x4.ToLeft();
        }

        #endregion
    }
}