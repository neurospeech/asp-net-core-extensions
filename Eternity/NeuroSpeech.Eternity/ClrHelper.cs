using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading;

namespace NeuroSpeech.Eternity
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public class ActivityAttribute: Attribute
    {

    }

    public class ClrHelper
    {

        public static ClrHelper Instance = new ClrHelper();
        private readonly ModuleBuilder moduleBuilder;
        private int id = 0;

        public ClrHelper()
        {
            var a = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("EternityWorkflows"), AssemblyBuilderAccess.RunAndCollect);
            this.moduleBuilder = a.DefineDynamicModule("EternityWorkflows");
        }

        static ConcurrentDictionary<Type, Type> derived = new ConcurrentDictionary<Type, Type>();

        internal Type GetDerived(Type type)
        {
            return derived.GetOrAdd(type, (x) => {
                lock (derived)
                {
                    return derived.GetOrAdd(x, Create);
                }
            });
        }

        private Type Create(Type type)
        {
            var dt = moduleBuilder.DefineType(type.Name + "Derived" + Interlocked.Increment(ref id),
               TypeAttributes.Public | TypeAttributes.Class, type);

            dt.DefineDefaultConstructor(MethodAttributes.Public);


            foreach (var method in type.GetMethods())
            {
                var a = method.GetCustomAttribute<ActivityAttribute>();
                if (a == null)
                {
                    continue;
                }

                if (!method.IsVirtual)
                    throw new InvalidOperationException($"Activity method must be virtual {method.DeclaringType.FullName}.{method.Name}");

                CreateMethod(dt, method);
            }

            return dt.CreateTypeInfo();
        }

        private void CreateMethod(TypeBuilder type, MethodInfo method)
        {
            var rootType = method.DeclaringType;

            bool hasReturnValue = method.ReturnType.IsConstructedGenericType;

            var pa = method.GetParameters().Select(p => p.ParameterType).ToArray();

            var om = type.DefineMethod(method.Name,
                MethodAttributes.Public
                | MethodAttributes.HideBySig
                | MethodAttributes.Virtual,
                method.CallingConvention,
                method.ReturnType,
                pa);


            type.DefineMethodOverride(om, method);

            var il = om.GetILGenerator();

            typeof(EternityContext).GetMethods().FirstOrDefault(x => x.Name == "ScheduleAsync");
        }
    }
}
