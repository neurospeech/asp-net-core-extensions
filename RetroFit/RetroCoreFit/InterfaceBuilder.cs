using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net.Http;
using System.Reflection;
using System.Reflection.Emit;

namespace RetroCoreFit
{


    public class InterfaceBuilder
    {
        private InterfaceBuilder()
        {

        }

        public static InterfaceBuilder Instance = new InterfaceBuilder();

        private Dictionary<Type, object> services = new Dictionary<Type, object>();

        public T Build<T>(Uri baseUri, HttpClient client = null, Type serviceType = null)
            where T: class
        {
            Type type = typeof(T);
            if (services.TryGetValue(type, out object s)) {
                return (s as T);
            }
            BaseService serviceInterface = CreateInstance(type, serviceType);
            serviceInterface.client = client ?? new HttpClient();
            serviceInterface.interfaceType = type;
            services[type] = serviceInterface;
            serviceInterface.BaseUrl = baseUri;
            return serviceInterface as T;
        }

        private AssemblyBuilder assemblyBuilder;
        private ModuleBuilder moduleBuilder;

        private BaseService CreateInstance(Type type , Type serviceType)
        {

            serviceType = serviceType ?? typeof(BaseService);

            if (!typeof(BaseService).IsAssignableFrom(serviceType))
                throw new ArgumentException($"ServiceType must derive from class {nameof(BaseService)}");

            assemblyBuilder = assemblyBuilder ?? AssemblyBuilder.DefineDynamicAssembly(
                new System.Reflection.AssemblyName($"RetroCoreFit2_{DateTime.UtcNow.Ticks}"), AssemblyBuilderAccess.RunAndCollect);
            moduleBuilder = moduleBuilder ?? assemblyBuilder.DefineDynamicModule("RetroCoreFit2");

            Dictionary<string, RestCall> methods = new Dictionary<string, RestCall>();

            TypeBuilder typeBuilder = moduleBuilder.DefineType(
                $"A._{type.Name}_{DateTime.UtcNow.Ticks}", 
                System.Reflection.TypeAttributes.Public | TypeAttributes.Class);

            typeBuilder.SetParent(serviceType);

            MethodInfo invokeMethod = typeof(BaseService).GetMethod("Invoke");

            typeBuilder.AddInterfaceImplementation(type);

            List<RestParameter> headerList = new List<RestParameter>();

            foreach (var property in type.GetProperties())
            {
                var px = GenerateProperty(typeBuilder, property);
                headerList.Add(new RestParameter {
                    Type = property.GetCustomAttribute<HeaderAttribute>(),
                    Value =property
                });
            }

            foreach (var method in type.GetMethods().Where(x=>!x.IsSpecialName)) {

                var pas = method.GetParameters().Select(x => x.ParameterType).ToArray();

                var m = typeBuilder.DefineMethod(
                        method.Name,
                        method.Attributes ^ MethodAttributes.Abstract,
                        method.CallingConvention,
                        method.ReturnType,
                        method.ReturnParameter.GetRequiredCustomModifiers(),
                        method.ReturnParameter.GetOptionalCustomModifiers(),
                        method.GetParameters().Select(p => p.ParameterType).ToArray(),
                        method.GetParameters().Select(p => p.GetRequiredCustomModifiers()).ToArray(),
                        method.GetParameters().Select(p => p.GetOptionalCustomModifiers()).ToArray()
                        );



                var ilGen = m.GetILGenerator();

                var httpMethod = method.GetCustomAttribute<HttpMethodAttribute>();

                string plist = string.Join(",", method.GetParameters().Select(x => x.ParameterType.FullName));
                string uniqueName = $"{method.Name}:{plist}";

                List<RestAttribute> rplist = new List<RestAttribute>();

                ilGen.Emit(OpCodes.Nop);
                ilGen.Emit(OpCodes.Ldarg_0);
                ilGen.Emit(OpCodes.Ldstr, uniqueName);

                var pls = method.GetParameters();

                ilGen.Emit(OpCodes.Ldc_I4, pls.Length);

                ilGen.Emit(OpCodes.Newarr, typeof(object));

                for (var i = 0; i < pls.Length; i++)
                {
                    var mp = pls[i];

                    var ra = mp.GetCustomAttribute<RestAttribute>();

                    if (ra is ParamAttribute n) {
                        if (n.Name == null) {
                            n.Name = mp.Name;
                        }
                    }

                    rplist.Add(ra ?? throw 
                        new InvalidOperationException($"Parameter must be decorated with Query, Form, Header, Cookie, Path, Body or MultipartBody"));

                    ilGen.Emit(OpCodes.Dup);
                    ilGen.Emit(OpCodes.Ldc_I4, i);
                    ilGen.Emit(OpCodes.Ldarg_S, i+1);
                    if (mp.ParameterType.IsValueType) {
                        ilGen.Emit(OpCodes.Box, mp.ParameterType);
                    }
                    ilGen.Emit(OpCodes.Stelem_Ref);
                }

                var retType = method.ReturnType.GetGenericArguments()[0];

                ilGen.EmitCall(OpCodes.Call, invokeMethod.MakeGenericMethod(retType), Type.EmptyTypes);
                ilGen.Emit(OpCodes.Ret);

                typeBuilder.DefineMethodOverride(m,method);

                methods[uniqueName] = new RestCall {
                    Path = httpMethod.Name,
                    Method = httpMethod.Method,
                    Attributes = rplist.ToArray()
                };




            }

            BaseService si = Activator.CreateInstance(typeBuilder.CreateType()) as BaseService;
            si.Methods = methods;
            si.Headers = headerList.ToArray();
            return si;
        }

        #region Generate Property
        private MethodInfo GenerateProperty(TypeBuilder typeBuilder, PropertyInfo property)
        {
            var p = typeBuilder.DefineProperty(property.Name, PropertyAttributes.None, property.PropertyType, null);
            var fname = $"_{property.Name}";
            var fld = typeBuilder.DefineField(fname, property.PropertyType, FieldAttributes.Private);

            var m = typeBuilder.DefineMethod(property.GetGetMethod().Name,
                MethodAttributes.Public | MethodAttributes.Virtual,
                property.PropertyType,
                null);

            var il = m.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldfld, fld);
            il.Emit(OpCodes.Ret);

            typeBuilder.DefineMethodOverride(m, property.GetGetMethod());
            // p.SetGetMethod(m);

            var getMethod = m;

            m = typeBuilder.DefineMethod(property.GetSetMethod().Name, MethodAttributes.Public | MethodAttributes.Virtual,
                property.GetSetMethod().ReturnType, new Type[] { property.PropertyType });
            il = m.GetILGenerator();
            il.Emit(OpCodes.Nop);
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Stfld, fld);
            il.Emit(OpCodes.Ret);
            typeBuilder.DefineMethodOverride(m, property.GetSetMethod());
            // p.SetSetMethod(m);

            return getMethod;
        } 
        #endregion
    }
}
