using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace NeuroSpeech.Eternity
{
    internal static class MethodHelper
    {

        private static MethodInfo methodRunAsyncOfT = typeof(MethodHelper).GetMethod(nameof(RunAsyncOfT));

        public static async Task<string> RunAsyncOfT<T>(
            MethodInfo method, 
            object target, 
            object[] parameters,
            System.Text.Json.JsonSerializerOptions options = default)
        {
            var r = (await (method.Invoke(target, parameters) as Task<T>));
            return JsonSerializer.Serialize(r, options);
        }

        public static async Task<string> RunAsync(
            this MethodInfo method, 
            object target, 
            object[] parameters,
            System.Text.Json.JsonSerializerOptions options = null)
        {
            if (method.ReturnType.IsConstructedGenericType)
            {
                var returnType = method.ReturnType.GenericTypeArguments[0];

                return await (methodRunAsyncOfT.MakeGenericMethod(returnType).Invoke(target, new object[] { 
                    target,
                    parameters,
                    options
                }) as Task<string>);
            }

            await (method.Invoke(target, parameters) as Task);
            return "";
        }

    }

    internal static class ValueTupleHelper
    {

        public static object GetTupleValue(this object item, Type type, int index)
        {
            return type.GetProperty($"Item{index}").GetValue(item);
        }

        public static object ToValueTuple(this MethodInfo method, object[] parameters)
        {
            var pas = method.GetParameters();
            var arg = new List<object>();
            var types = new List<Type>();
            if (pas.Length > 6)
                throw new NotSupportedException();
            for (int i = 0; i < pas.Length; i++)
            {
                var p = pas[i];
                types.Add(p.ParameterType);
                if (p.GetCustomAttribute<InjectAttribute>() != null)
                {
                    arg.Add(null);
                    continue;
                }
                if (i < parameters.Length)
                {
                    arg.Add(parameters[i]);
                    continue;
                }
                if (p.HasDefaultValue)
                {
                    arg.Add(p.RawDefaultValue);
                    continue;
                }
                throw new NotSupportedException();
            }
       
            var factory = typeof(Tuple).GetMethod("Create", types.ToArray());
            return factory.Invoke(null, arg.ToArray());
        }
    }
}
