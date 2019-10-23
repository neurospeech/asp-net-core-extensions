using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace NeuroSpeech.EFCoreLiveMigration
{
    internal static class StringExtensions
    {

        public static bool ContainsIgnoreCase(this string text, string test) {
            if (string.IsNullOrWhiteSpace(text))
                return false;
            if (string.IsNullOrWhiteSpace(test))
                return false;
            return text.IndexOf(test, StringComparison.OrdinalIgnoreCase) != -1;
        }

        public static bool EqualsIgnoreCase(this string text, string test) {
            if (string.IsNullOrWhiteSpace(text))
                return string.IsNullOrWhiteSpace(test);
            if (string.IsNullOrWhiteSpace(test))
                return false;
            return text.Equals(test, StringComparison.OrdinalIgnoreCase);
        }

        public static string GetColumnType(this IProperty property) {

            string type = property.Relational().ColumnType;
            return type.Split('(')[0].Trim();
        }

        public static string[] GetOldNames(this IProperty property)
        {
            if (property.PropertyInfo == null)
            {
                return null;
            }
            var oa = property.PropertyInfo.GetCustomAttributes<OldNameAttribute>();

            return oa.Select(x=>x.Name).ToArray();
        }

        public static string ToJoinString(this IEnumerable<string> list, string separator = ", ") {
            return string.Join(separator, list);
        }

        
    }
}
