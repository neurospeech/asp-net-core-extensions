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
}
