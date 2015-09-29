using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

#if MVVM
namespace DevExpress.Mvvm.Native {
#else
namespace DevExpress.Internal {
#endif
#if MVVM
    public
#endif
    interface IImmutableStack<out T> : IEnumerable<T> {
        T Peek();
        IImmutableStack<T> Pop();
        bool IsEmpty { get; }
    }
#if MVVM
    public
#endif
    static class ImmutableStack {
        class EmptyStack<T> : IImmutableStack<T> {
            public static readonly IImmutableStack<T> Instance = new EmptyStack<T>();
            EmptyStack() { }
            T IImmutableStack<T>.Peek() { throw new InvalidOperationException(); }
            IImmutableStack<T> IImmutableStack<T>.Pop() { throw new InvalidOperationException();  }

            IEnumerator<T> IEnumerable<T>.GetEnumerator() {
                yield break;
            }
            IEnumerator IEnumerable.GetEnumerator() {
                yield break;
            }
            bool IImmutableStack<T>.IsEmpty { get { return true; } }
        }

        class SimpleStack<T> : IImmutableStack<T> {
            readonly T head;
            readonly IImmutableStack<T> tail;
            T IImmutableStack<T>.Peek() { return head; }
            IImmutableStack<T> IImmutableStack<T>.Pop() { return tail; }
            bool IImmutableStack<T>.IsEmpty { get { return false; } }
            public SimpleStack(T head, IImmutableStack<T> tail) {
                this.head = head;
                this.tail = tail;
            }
            IEnumerator<T> IEnumerable<T>.GetEnumerator() {
                return GetEnumeratorCore();
            }
            IEnumerator IEnumerable.GetEnumerator() {
                return GetEnumeratorCore();
            }
            IEnumerator<T> GetEnumeratorCore() {
                return LinqExtensions.Unfold<IImmutableStack<T>>(this, x => x.Pop(), x => x.IsEmpty)
                    .Select(x => x.Peek()).GetEnumerator();
            }
        }
        public static IImmutableStack<T> Empty<T>() {
            return EmptyStack<T>.Instance;
        }
        public static IImmutableStack<T> Push<T>(this IImmutableStack<T> stack, T item) {
            return new SimpleStack<T>(item, stack);
        }
        public static IImmutableStack<T> PushMultiple<T>(this IImmutableStack<T> source, IEnumerable<T> items) {
            return items.Aggregate(source, (stack, x) => stack.Push(x));
        }
        public static IImmutableStack<T> Reverse<T>(this IImmutableStack<T> stack) {
            var reverse = Empty<T>();
            while(!stack.IsEmpty) {
                reverse = reverse.Push(stack.Peek());
                stack = stack.Pop();
            }
            return reverse;
        }
        public static IImmutableStack<T> ToImmutableStack<T>(this IEnumerable<T> source) {
            return Empty<T>().PushMultiple(source.Reverse());
        }
    }
}
