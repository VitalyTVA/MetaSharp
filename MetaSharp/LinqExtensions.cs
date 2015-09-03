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
        public static string ConcatStrings(this IEnumerable<string> source, string delimeter = "\r\n") {
            var builder = new StringBuilder(source.FirstOrDefault());
            foreach(var text in source.Tail()) {
                builder.Append(delimeter);
                builder.Append(text);
            }
            return builder.ToString();
        }
    }
}
