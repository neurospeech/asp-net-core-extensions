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
