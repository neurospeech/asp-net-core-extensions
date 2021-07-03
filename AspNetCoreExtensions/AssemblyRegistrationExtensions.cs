using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace NeuroSpeech
{
    public class AssemblyRegistration
    {
        public Assembly Assembly;
        public bool ApplicationPart;
        public Type StartupType;
    }

    public interface IStartup
    {
        void Configure(IServiceCollection services);
    }

    public class AssemblyRegistrationList
    {
        public readonly List<Assembly> List = new List<Assembly>();
        private readonly List<Assembly> Registered = new List<Assembly>();
        private readonly IServiceCollection services;

        public AssemblyRegistrationList(IServiceCollection services)
        {
            this.services = services;
        }

        public void Add(Assembly assembly, bool registerApplicationPart, Type startupType = null)
        {
            var r = Registered.FirstOrDefault(x => x == assembly);
            if (r != null)
                return;
            Registered.Add(assembly);
            if (registerApplicationPart)
            {
                List.Add(assembly);
            }
            if (startupType != null)
            {
                var s = Activator.CreateInstance(startupType) as IStartup;
                s.Configure(services);
            }
        }

    }


    [ExcludeFromCodeCoverage]
    public static class AssemblyRegistrationExtensions
    {
        public static IMvcBuilder RegisterApplicationParts(this IMvcBuilder mvcBuilder, AssemblyRegistrationList list, params Assembly[] other )
        {
            foreach(var o in other)
            {
                mvcBuilder.AddApplicationPart(o);
            }
            foreach (var a in list.List)
            {
                mvcBuilder.AddApplicationPart(a);
            }
            return mvcBuilder;
        }
        public static AssemblyRegistrationList RegisterAssemblies(this IServiceCollection services, AppDomain domain = null)
        {
            var all = (domain ?? AppDomain.CurrentDomain).GetAssemblies()
                .Select(x => new {
                    Assembly = x,
                    Core = x.GetCustomAttribute<RegisterAssemblyAttribute>()
                })
            .Where(x => x.Core != null);
            var list = new AssemblyRegistrationList(services);
            foreach (var item in all)
            {
                list.Add(item.Assembly, item.Core?.RegisterParts ?? false, item.Core?.StartupType);
            }

            list.Add(Assembly.GetEntryAssembly(), true);
            return list;
        }

    }
}
