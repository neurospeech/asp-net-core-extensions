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

    /// <summary>
    /// Registers current assembly with Assembly parts
    /// </summary>
    [System.AttributeUsage(AttributeTargets.Assembly, Inherited = false, AllowMultiple = false)]


    public sealed class RegisterAssemblyAttribute : Attribute
    {

        public readonly bool RegisterParts;
        public readonly Type StartupType;

        public RegisterAssemblyAttribute(bool registerParts = true, Type startupType = null)
        {
            this.StartupType = startupType;
            this.RegisterParts = registerParts;
        }

    }

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

        /// <summary>
        /// Reads embedded string resource from given assembly
        /// </summary>
        /// <param name="assembly"></param>
        /// <param name="name"></param>
        /// <param name="encoding"></param>
        /// <returns></returns>
        public static async Task<string> GetStringResourceAsync(
            this Assembly assembly,
            string name,
            Encoding encoding = null)
        {
            using (var rs = assembly.GetManifestResourceStream(name))
            {
                using (var reader = new StreamReader(rs, encoding ?? Encoding.UTF8))
                {
                    return await reader.ReadToEndAsync();
                }
            }
        }

        /// <summary>
        /// Reads embedded string resource from given assembly
        /// </summary>
        /// <param name="assembly"></param>
        /// <param name="name"></param>
        /// <param name="encoding"></param>
        /// <returns></returns>
        public static string GetStringResource(
            this Assembly assembly,
            string name,
            Encoding encoding = null)
        {
            using (var rs = assembly.GetManifestResourceStream(name))
            {
                using (var reader = new StreamReader(rs, encoding ?? Encoding.UTF8))
                {
                    return reader.ReadToEnd();
                }
            }
        }

        /// <summary>
        /// Registers given assembly types for DI
        /// </summary>
        /// <param name="services"></param>
        /// <param name="assembly"></param>
        public static void RegisterAssembly(this IServiceCollection services, Assembly assembly)
        {

            foreach (var type in assembly.GetExportedTypes())
            {

                try
                {
                    var a = type.GetCustomAttribute<DIRegisterAttribute>();
                    if (a != null)
                    {
                        Type baseType = a.BaseType;

                        Func<IServiceProvider, object> factory = null;
                        if (a.Factory != null)
                        {
                            BaseDIFactory f = Activator.CreateInstance(a.Factory) as BaseDIFactory;
                            factory = (sp) => f.CreateService(sp);
                        }

                        if (baseType != null)
                        {
                            services.Add(new ServiceDescriptor(baseType, type, a.Type));
                        }
                        else
                        {
                            if (factory != null)
                            {
                                services.Add(new ServiceDescriptor(type, factory, a.Type));
                            }
                            else
                            {
                                services.Add(new ServiceDescriptor(type,type, a.Type));
                            }
                        }
                    }
                }
                catch { }
            }

        }

    }
}
