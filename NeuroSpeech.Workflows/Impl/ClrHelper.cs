using DurableTask.Core;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading;

namespace NeuroSpeech.Workflows.Impl
{
    public class ClrHelper
    {

        public static ClrHelper Instance = new ClrHelper();
        private readonly ModuleBuilder moduleBuilder;
        private int id = 0;
        

        public ClrHelper()
        {
            var a = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("ClrWorkflows"), AssemblyBuilderAccess.RunAndCollect);
            this.moduleBuilder = a.DefineDynamicModule("ClrWorkflows");
        }


        public (Func<string, IServiceProvider, object> factory, string derived, Type[] activities) Factory(Type type)
        {
            Dictionary<string, (Type type, MethodInfo method)> types = new Dictionary<string, (Type, MethodInfo)>();

            object Search(string name, IServiceProvider sp)
            {
                if (!types.TryGetValue(name, out var t))
                    throw new ArgumentException($"No type found for {name}");
                if (t.method == null)
                {
                    return sp.Build(t.type);
                }

                var activity =  Activator.CreateInstance(t.type) as IWorkflowActivityInit;
                activity.Set(sp, t.method);
                return activity;
            }

            List<Type> activities = new List<Type>();

            var dt = moduleBuilder.DefineType(type.Name + "Derived" + Interlocked.Increment(ref id), 
                TypeAttributes.Public | TypeAttributes.Class, type);

            dt.DefineDefaultConstructor(MethodAttributes.Public);


            foreach(var method in type.GetMethods())
            {
                if (!method.IsVirtual)
                    continue;
                var a = method.GetCustomAttribute<ActivityAttribute>();
                if (a == null)
                    continue;

                var at = CreateMethod(dt, method);
                types[at.FullName] = (at, method);
                activities.Add(at);
            }

            var innerClass = dt.CreateTypeInfo();

            var (inputType, outputType) = type.Get2GenericArguments();

            var wrapper = typeof(WorkflowExecutor<,,>).MakeGenericType(innerClass, inputType, outputType);
            types[type.FullName] = (wrapper, null);

            return (Search, type.FullName, activities.ToArray());
        }

        private Type CreateMethod(TypeBuilder type, MethodInfo method)
        {

            Type activityType = CreateProxyClass(type, method);
            

            var pa = method.GetParameters().Select(p => p.ParameterType).ToArray();

            var input = pa[0];

            var om = type.DefineMethod(method.Name, 
                MethodAttributes.Public 
                | MethodAttributes.HideBySig 
                | MethodAttributes.Virtual,
                method.CallingConvention, 
                method.ReturnType, 
                pa);

            type.DefineMethodOverride(om, method);


            var rt = method.ReturnType.GetGenericArguments()[0];

            var il = om.GetILGenerator();
            var fld = typeof(Workflow<,>).GetField("context");
            var callTask = type
                .BaseType
                .BaseType
                .GetMethods(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy)
                    .First(m => m.Name == "CallTaskAsync")
                .MakeGenericMethod(input, activityType, rt);

            // load this...
            il.Emit(OpCodes.Ldarg_0);
            

            // load first argument
            il.Emit(OpCodes.Ldarg_1);

            // execute method...
            il.Emit(OpCodes.Callvirt, callTask);
            il.Emit(OpCodes.Ret);
            

            return activityType;

        }

        private Type CreateProxyClass(TypeBuilder type, MethodInfo method)
        {
            var moduleBuilder = type.Module as ModuleBuilder;
            var rt = method.ReturnType.GetGenericArguments()[0];
            var input = method.GetParameters()[0];

            var pa = method.GetParameters();

            var workflowType = type.BaseType;

            var baseType = typeof(WorkflowActivity<,,>).MakeGenericType(workflowType, input.ParameterType, rt);

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
