using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace NeuroSpeech.Eternity
{

    internal static class MethodHelper
    {

        private static MethodInfo methodRunAsyncOfT = typeof(MethodHelper).GetMethod(nameof(RunAsyncOfT));

        public static object InvokeNotOverride(this MethodInfo methodInfo,
            object targetObject, params object[] arguments)
        {
            var parameters = methodInfo.GetParameters();

            if (parameters.Length == 0)
            {
                if (arguments != null && arguments.Length != 0)
                    throw new Exception("Arguments cont doesn't match");
            }
            else
            {
                if (parameters.Length != arguments.Length)
                    throw new Exception("Arguments cont doesn't match");
            }

            Type returnType = null;
            if (methodInfo.ReturnType != typeof(void))
            {
                returnType = methodInfo.ReturnType;
            }

            var type = targetObject.GetType();
            var dynamicMethod = new DynamicMethod("", returnType,
                    new Type[] { type, typeof(Object) }, type);

            var iLGenerator = dynamicMethod.GetILGenerator();
            iLGenerator.Emit(OpCodes.Ldarg_0); // this

            for (var i = 0; i < parameters.Length; i++)
            {
                var parameter = parameters[i];

                iLGenerator.Emit(OpCodes.Ldarg_1); // load array argument

                // get element at index
                iLGenerator.Emit(OpCodes.Ldc_I4_S, i); // specify index
                iLGenerator.Emit(OpCodes.Ldelem_Ref); // get element

                var parameterType = parameter.ParameterType;
                if (parameterType.IsPrimitive)
                {
                    iLGenerator.Emit(OpCodes.Unbox_Any, parameterType);
                }
                else if (parameterType == typeof(object))
                {
                    // do nothing
                }
                else
                {
                    iLGenerator.Emit(OpCodes.Castclass, parameterType);
                }
            }

            iLGenerator.Emit(OpCodes.Call, methodInfo);
            iLGenerator.Emit(OpCodes.Ret);

            return dynamicMethod.Invoke(null, new object[] { targetObject, arguments });
        }

        public static async Task<string> RunAsyncOfT<T>(
            MethodInfo method, 
            object target, 
            object[] parameters,
            System.Text.Json.JsonSerializerOptions options = default)
        {
            var r = (await (method.InvokeNotOverride(target, parameters) as Task<T>));
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
                    method,
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
