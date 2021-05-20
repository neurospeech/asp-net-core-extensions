using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace NeuroSpeech.EFCoreLiveMigration
{
    public static class StringExtensions
    {

        public static (int? length, int? @decimal) GetColumnDataLength(this IProperty property)
        {
            string type = property.GetColumnType();
            var tokens = type.Split('(');
            if(tokens.Length > 1)
            {
                tokens = tokens[1]
                    .Trim()
                    .Trim(')')
                    .Split(',');
                if (tokens.Length > 1)
                {
                    var first = tokens[0].Trim();
                    var second = tokens[1].Trim();
                    if(int.TryParse(first, out var f1))
                    {
                        if(int.TryParse(second, out var s1))
                        {
                            return (f1, s1);
                        }
                        return (f1, null);
                    }
                }
                if(tokens.Length == 1)
                {
                    if (int.TryParse(tokens[0], out var l))
                        return (l, null);
                }
            }
            return (null, null);
        }

        public static string GetColumnTypeForSql(this IProperty property) {

            string type = property.GetColumnType();
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

        public static string ToJoinString(
            this IEnumerable<string> list, 
            Func<string,string> escape,
            string separator = ", ")
        {
            return string.Join(separator, list.Select(x => escape(x)));
        }

    }
}
