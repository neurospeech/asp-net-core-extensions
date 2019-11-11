using System;
using System.Collections.Generic;
using System.Text;

namespace EFCoreBulk
{
    internal static class EFStringHelper
    {
        public static StringBuilder AppendWithSeparator(this StringBuilder sb, string text, string separator = ", ")
        {
            if (sb.Length > 0)
            {
                sb.Append(separator);
            }
            return sb.Append(text);
        }

        public static RT Reduce<T, RT>(this IEnumerable<T> list, Func<T, RT, int, RT> func, RT start)
        {
            RT x = start;
            int i = 0;
            foreach(var item in list)
            {
                x = func(item, x, i);
            }
            return x;
        }

        public static StringBuilder ReduceToString<T>(this IEnumerable<T> list, Func<T, StringBuilder, int, StringBuilder> func, StringBuilder start = null)
        {
            start = start ?? new StringBuilder();
            int index = 0;
            foreach(var item in list)
            {
                start = func(item, start, index);
            }
            return start;
        }
    }
}
