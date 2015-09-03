using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MetaSharp {
    public static class LinqExtensions {
        public static IEnumerable<T> Yield<T>(this T item) {
            yield return item;
        }
        public static void ForEach<T>(this IEnumerable<T> source, Action<T> action) {
            foreach(var item in source) {
                action(item);
            }
        }
        public static IEnumerable<T> Tail<T>(this IEnumerable<T> source) {
            return source.Skip(1);
        }
        public static IEnumerable<T> InsertDelimeter<T>(this IEnumerable<T> source, T delimeter) {
            var en = source.GetEnumerator();
            if(en.MoveNext())
                yield return en.Current;
            while(en.MoveNext()) {
                yield return delimeter;
                yield return en.Current;
            }
        }
        public static string ConcatStrings(this IEnumerable<string> source) {
            return source
                .Aggregate(new StringBuilder(), (builder, text) => builder.Append(text))
                .ToString();
        }
    }
}
