using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace MetaSharp.Native {
    public static class LinqExtensions {
        public static IEnumerable<T> Yield<T>(this T item) {
            yield return item;
        }

        public static T[] YieldToArray<T>(this T item)
            => new[] { item };

        public static void ForEach<T>(this IEnumerable<T> source, Action<T> action) {
            foreach(var item in source) {
                action(item);
            }
        }

        public static IEnumerable<T> Tail<T>(this IEnumerable<T> source)
            => source.Skip(1);

        public static IEnumerable<T> InsertDelimeter<T>(this IEnumerable<T> source, T delimeter) {
            var en = source.GetEnumerator();
            if(en.MoveNext())
                yield return en.Current;
            while(en.MoveNext()) {
                yield return delimeter;
                yield return en.Current;
            }
        }

        public static IEnumerable<T> Unfold<T>(T start, Func<T, T> next, Func<T, bool> stop = null) {
            stop = stop ?? (x => x == null);
            for(var current = start; !stop(current); current = next(current)) {
                yield return current;
            }
        }

        public static T ToValue<T>(this object[] values)
            => (T)values.Single();
        public static Tuple<T1, T2> ToValues<T1, T2>(this object[] values) {
            if(values.Length != 2)
                throw new InvalidOperationException();
            return Tuple.Create((T1)values[0], (T2)values[1]);
        }
    }
    public static class StringExtensions {
        public static string ToCamelCase(this string s) {
            //TODO correct camel case
            return char.ToLower(s[0]) + s.Substring(1);
        }
        public static string ReplaceEnd(this string s, string oldEnd, string newEnd) {
            if(!s.EndsWith(oldEnd))
                throw new InvalidOperationException();
            return s.Substring(0, s.Length - oldEnd.Length) + newEnd;
        }
        public static string ConcatStrings(this IEnumerable<string> source)
            => source
                .Aggregate(new StringBuilder(), (builder, text) => builder.Append(text))
                .ToString();

        public static string ConcatStrings(this IEnumerable<string> source, string delimeter)
            => source
                .InsertDelimeter(delimeter)
                .ConcatStrings();


        public static string ConcatStringsWithNewLines(this IEnumerable<string> source)
            => source.ConcatStrings(Environment.NewLine);

        public static string AddTabs(this string s, int tabs) {
            var indent = new string(' ', tabs * 4);
            return s
                .Split(Environment.NewLine.YieldToArray(), StringSplitOptions.None)
                .Select(str => string.IsNullOrEmpty(str) ? string.Empty : indent + str)
                .ConcatStringsWithNewLines();
        }

        public static string RemoveEmptyLines(this string s)
            => s.Split(Environment.NewLine.YieldToArray(), StringSplitOptions.RemoveEmptyEntries)
                .ConcatStringsWithNewLines();
    }
    public static class ExpressionExtensions {
        public static string GetPropertyName<T>(Expression<Func<T>> expression) {
            return GetPropertyNameFast(expression);
        }
        public static string GetPropertyNameFast(LambdaExpression expression) {
            MemberExpression memberExpression = expression.Body as MemberExpression;
            if(memberExpression == null) {
                throw new ArgumentException("MemberExpression is expected in expression.Body", "expression");
            }
            var member = memberExpression.Member;
            return member.Name;
        }
    }
    [DebuggerNonUserCode]
    public static class MayBe {
        public static TR With<TI, TR>(this TI input, Func<TI, TR> evaluator)
            where TI : class {
            if(input == null)
                return default(TR);
            return evaluator(input);
        }
        public static TR With<TI, TR>(this TI? input, Func<TI, TR> evaluator)
            where TI : struct {
            if(input == null)
                return default(TR);
            return evaluator(input.Value);
        }
        public static TR Return<TI, TR>(this TI? input, Func<TI?, TR> evaluator, Func<TR> fallback) where TI : struct {
            if(!input.HasValue)
                return fallback != null ? fallback() : default(TR);
            return evaluator(input.Value);
        }
        public static TR Return<TI, TR>(this TI input, Func<TI, TR> evaluator, Func<TR> fallback) where TI : class {
            if(input == null)
                return fallback != null ? fallback() : default(TR);
            return evaluator(input);
        }
        public static bool ReturnSuccess<TI>(this TI input) where TI : class {
            return input != null;
        }
        public static bool ReturnSuccess<TI>(this TI? input) where TI : struct {
            return input != null;
        }
        public static TI If<TI>(this TI input, Func<TI, bool> evaluator) where TI : class {
            if(input == null)
                return null;
            return evaluator(input) ? input : null;
        }
        public static TI? If<TI>(this TI? input, Func<TI, bool> evaluator) where TI : struct {
            if(input == null)
                return null;
            return evaluator(input.Value) ? input : null;
        }
        public static TI Do<TI>(this TI input, Action<TI> action) where TI : class {
            if(input == null)
                return null;
            action(input);
            return input;
        }
        public static TI? Do<TI>(this TI? input, Action<TI> action) where TI : struct {
            if(input == null)
                return null;
            action(input.Value);
            return input;
        }
    }

    public abstract class Either<TLeft, TRight> {
        Either() { }
        internal class Left : Either<TLeft, TRight> {
            internal readonly TLeft Value;
            internal Left(TLeft value) {
                Value = value;
            }
        }
        internal class Right : Either<TLeft, TRight> {
            internal readonly TRight Value;
            internal Right(TRight value) {
                Value = value;
            }
        }
    }
    public static class Either {
        //TODO use friend access diagnostics - only this class can access Either's internals
        public static Either<TLeft, TRight> Left<TLeft, TRight>(TLeft value) {
            return new Either<TLeft, TRight>.Left(value);
        }
        public static Either<TLeft, TRight> Right<TLeft, TRight>(TRight value) {
            return new Either<TLeft, TRight>.Right(value);
        }
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
            var leftValue = value as Either<TLeft, TRight>.Left;
            if(leftValue != null)
                return left(leftValue.Value);
            return right((value as Either<TLeft, TRight>.Right).Value);
        }
    }
}
