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
    }
    public static class StringExtensions {
        public static string ReplaceEnd(this string s, string oldEnd, string newEnd) {
            if(!s.EndsWith(oldEnd))
                throw new InvalidOperationException();
            return s.Substring(0, s.Length - oldEnd.Length) + newEnd;
        }
        public static string ConcatStrings(this IEnumerable<string> source)
            => source
                .Aggregate(new StringBuilder(), (builder, text) => builder.Append(text))
                .ToString();

        public static string ConcatStringsWithNewLines(this IEnumerable<string> source)
            => source
                .InsertDelimeter(Environment.NewLine)
                .ConcatStrings();

        public static string AddIndent(this string s, int indentLength) {
            var indent = new string(' ', indentLength);
            return s
                .Split(Environment.NewLine.YieldToArray(), StringSplitOptions.None)
                .Select(str => string.IsNullOrEmpty(str) ? string.Empty : indent + str)
                .ConcatStringsWithNewLines();
        }
    }
}
