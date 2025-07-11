using System;
using System.Collections.Generic;

namespace Rules.Utilities
{
    internal static class Execute
    {
        public static IEnumerable<T> Enumerable_Execute<T>(this IEnumerable<T> items, IEnumerable<Action<T>> methods)
        {
            foreach (var item in items)
            {
                foreach (var method in methods)
                    method(item);

                yield return item;
            }
        }
        public static IEnumerable<T> Enumerable_Execute<T>(this IEnumerable<T> items, Action<T> method)
        {
            foreach (var item in items)
                yield return item.Enumerable_Execute(method);
        }
        public static IEnumerable<T> Enumerable_Execute<T>(this T item, IEnumerable<Action<T>> methods)
        {
            foreach (var method in methods)
                yield return item.Enumerable_Execute(method);
        }
        public static T Enumerable_Execute<T>(this T item, Action<T> method)
        {
            method(item);
            return item;
        }
    }
}
