using System;
using System.Collections.Generic;
using System.Linq;
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

        public static string AddIndent(this string s, int indentLength) {//TODO replace with add tabs
            var indent = new string(' ', indentLength);
            return s
                .Split(Environment.NewLine.YieldToArray(), StringSplitOptions.None)
                .Select(str => string.IsNullOrEmpty(str) ? string.Empty : indent + str)
                .ConcatStringsWithNewLines();
        }

        public static string RemoveEmptyLines(this string s)
            => s.Split(Environment.NewLine.YieldToArray(), StringSplitOptions.RemoveEmptyEntries)
                .ConcatStringsWithNewLines();
    }
}
