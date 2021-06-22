using DurableTask.Core;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NeuroSpeech.Workflows.Impl
{
    public class ClrHelper
    {

        public static ClrHelper Instance = new ClrHelper();
        private readonly ModuleBuilder moduleBuilder;
        private readonly Dictionary<string, (Type type, MethodInfo method, Type[] argList)> types 
            = new Dictionary<string, (Type type, MethodInfo method, Type[] argList)>();
        private int id = 0;
        

        public ClrHelper()
        {
            var a = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("ClrWorkflows"), AssemblyBuilderAccess.RunAndCollect);
            this.moduleBuilder = a.DefineDynamicModule("ClrWorkflows");
        }

        public object Build(string name, IServiceProvider sp)
        {
            if (!types.TryGetValue(name, out var t))
                throw new ArgumentException($"No type found for {name}");
            if (t.method == null)
            {
                return sp.Build(t.type);
            }

            var activity = Activator.CreateInstance(t.type) as IWorkflowActivityInit;
            activity.Set(sp, t.method, t.argList);
            return activity;
        }

        public (Func<string, IServiceProvider, object> factory, string derived, Type[] activities) Factory(Type type)
        {
            List<Type> activities = new List<Type>();

            var dt = moduleBuilder.DefineType(type.Name + "Derived" + Interlocked.Increment(ref id), 
                TypeAttributes.Public | TypeAttributes.Class, type);

            dt.DefineDefaultConstructor(MethodAttributes.Public);

            foreach(var method in type.GetMethods())
            {
                var a = method.GetCustomAttribute<ActivityAttribute>();
                if (a == null) {
                    continue;
                }

                if (!method.IsVirtual)
                    throw new InvalidOperationException($"Activity method must be virtual {method.DeclaringType.FullName}.{method.Name}");

                var (at, argList) = CreateMethod(dt, method);
                types[at.FullName] = (at, method, argList);
                activities.Add(at);
            }

            var innerClass = dt.CreateTypeInfo();

            var (_, inputType, outputType) = type.Get3GenericArguments();

            var wrapper = typeof(WorkflowExecutor<,,>).MakeGenericType(innerClass, inputType, outputType);
            types[type.FullName] = (wrapper, null, null);

            return (Build, type.FullName, activities.ToArray());
        }

        private (Type type, Type[] argList) CreateMethod(TypeBuilder type, MethodInfo method)
        {
            Type input;
            bool isTuple = false;
            var inputParameterList = method.GetParameters()
                .Select(p => (Type: p.ParameterType, Attribute: p.GetCustomAttribute<InjectAttribute>()))
                .Where(p => p.Attribute == null)
                .ToList();
            var argList = inputParameterList.Select(p => p.Type).ToArray();
            var argCount = argList.Length;


            if (argCount == 1)
            {
                input = inputParameterList[0].Type;
            } else
            {
                isTuple = true;
                input = argList.ToTuple();
            }

            var rt = method.ReturnType.IsConstructedGenericType
                ? method.ReturnType.GetGenericArguments()[0]
                : typeof(string);
            Type activityType = CreateProxyClass(type, method, input, rt);
            

            var pa = method.GetParameters().Select(p => p.ParameterType).ToArray();



            string methodName = "CallTaskAsync";

            Type[] relayParams = null;


            if (inputParameterList.Count == 1)
            {
                relayParams = new Type[] { input, activityType, rt };
            } else
            {
                // create tuple....
                methodName = $"CallTupleAsync{argCount}";
                relayParams = new Type[argCount + 2];
                Array.Copy(argList, relayParams, argCount);
                relayParams[argCount] = activityType;
                relayParams[argCount + 1] = rt;
            }

            var om = type.DefineMethod(method.Name, 
                MethodAttributes.Public 
                | MethodAttributes.HideBySig 
                | MethodAttributes.Virtual,
                method.CallingConvention, 
                method.ReturnType, 
                pa);

            type.DefineMethodOverride(om, method);



            var il = om.GetILGenerator();
            var callTask = type
                .BaseType
                .BaseType
                .GetMethods(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy)
                    .First(m => m.Name == methodName)
                .MakeGenericMethod(relayParams);

            // load this...
            il.Emit(OpCodes.Ldarg_0);

            if (isTuple)
            {
                for (int i = 0; i < argCount; i++)
                {
                    switch (i)
                    {
                        case 0:
                            il.Emit(OpCodes.Ldarg_1);
                            continue;
                        case 1:
                            il.Emit(OpCodes.Ldarg_2);
                            continue;
                        case 3:
                            il.Emit(OpCodes.Ldarg_3);
                            continue;
                    }
                    il.Emit(OpCodes.Ldarg_S, (short)(i + 1));
                }
            }
            else
            {

                // load first argument
                il.Emit(OpCodes.Ldarg_1);
            }

            // execute method...
            il.Emit(OpCodes.Callvirt, callTask);
            il.Emit(OpCodes.Ret);
            

            return (activityType, argList);

        }

        private Type CreateProxyClass(TypeBuilder type, MethodInfo method, Type input, Type rt)
        {
            var moduleBuilder = type.Module as ModuleBuilder;
            // var rt = method.ReturnType.GetGenericArguments()[0];

            // var pa = method.GetParameters();

            var workflowType = type.BaseType;

            var baseType = typeof(WorkflowActivity<,,>).MakeGenericType(workflowType, input, rt);

            type = moduleBuilder.DefineType(type.Name + "_" + method.Name + "_Activity",
                TypeAttributes.Public | TypeAttributes.Class,
                baseType);

            // var c = type.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard | CallingConventions.HasThis, Type.EmptyTypes);
            type.DefineDefaultConstructor(MethodAttributes.Public);
            return type.CreateTypeInfo();

            

            //var baseType = typeof(WorkflowActivity<,>).MakeGenericType(input.ParameterType, rt);
            //type = moduleBuilder.DefineType(
            //    type.Name + "_" + method.Name + "_Acitivity" + Interlocked.Increment(ref id), 
            //    TypeAttributes.Public | TypeAttributes.Class);

            //type.SetParent(baseType);

            //var taskContextField = typeof(WorkflowActivity<,>).GetField("taskContext");

            //type.DefineDefaultConstructor(MethodAttributes.Public);

            //var om = baseType.GetMethod("ExecuteAsync");

            //var m = type.DefineMethod(om.Name,
            //    MethodAttributes.Public
            //    | MethodAttributes.HideBySig
            //    | MethodAttributes.Virtual, 
            //    om.CallingConvention, om.ReturnType, om.GetParameters().Select(t => t.ParameterType).ToArray());


            //type.DefineMethodOverride(m, om);

            //var il = m.GetILGenerator();


            //var serviceExt = typeof(ServiceProviderServiceExtensions);
            //var getRequiredService = serviceExt.GetMethod(nameof(ServiceProviderServiceExtensions.GetRequiredService), new Type[] { typeof(IServiceProvider), typeof(Type) });

            //// create new instance...
            //il.Emit(OpCodes.Newobj, workflowType);

            //// this is 0
            //// IServiceProvider is 1
            //// Input is 2

            //for (int i = 0; i < pa.Length; i++)
            //{
            //    var p = pa[i];
            //    if(p.ParameterType == typeof(TaskContext))
            //    {
            //        il.Emit(OpCodes.Ldarg_0);
            //        il.Emit(OpCodes.Ldfld, taskContextField);
            //        continue;
            //    }
            //    if(p.ParameterType == typeof(IServiceProvider))
            //    {
            //        il.Emit(OpCodes.Ldarg_1);
            //        continue;
            //    }
            //    if(p.ParameterType == input.ParameterType)
            //    {
            //        il.Emit(OpCodes.Ldarg_2);
            //        continue;
            //    }

            //    il.Emit(OpCodes.Ldarg_1);
            //    il.EmitType(p.ParameterType);
            //    il.Emit(OpCodes.Call, getRequiredService);
            //}

            //il.Emit(OpCodes.Callvirt, method);
            //il.Emit(OpCodes.Ret);

            return type.CreateTypeInfo();
        }
    }
}
