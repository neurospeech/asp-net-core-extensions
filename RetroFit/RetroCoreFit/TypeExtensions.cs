using System;
using System.Collections.Generic;
using System.Text;

namespace RetroCoreFit
{
    internal static class TypeExtensions
    {

        public static object ConvertFrom(this Type type, object source)
        {
            var t = Nullable.GetUnderlyingType(type) ?? type;
            try
            {
                return Convert.ChangeType(source, t);
            }
            catch (Exception ex)
            {
                throw new TypeConversionException(source, $"Failed to convert {source.GetType().FullName} to {t.FullName}", ex);
            }

        }

    }

    public class TypeConversionException : Exception
    {
        public object Value { get; }

        public TypeConversionException(object value, string message, Exception ex)
            : base(message, ex)
        {
            this.Value = value;
        }
    }
}
