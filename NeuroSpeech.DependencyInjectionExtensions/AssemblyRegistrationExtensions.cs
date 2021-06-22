using Microsoft.Extensions.DependencyInjection;
using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace NeuroSpeech
{

    [ExcludeFromCodeCoverage]
    public static class AssemblyRegistrationExtensions
    {

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
        public static void RegisterAssembly(
            this IServiceCollection services,
            Assembly assembly,
            Func<Type, DIRegisterAttribute> getRegisterAttribute = null)
        {

            getRegisterAttribute = getRegisterAttribute ?? ((t) => t.GetCustomAttribute<DIRegisterAttribute>());

            foreach (var type in assembly.GetExportedTypes())
            {

                try
                {
                    var a = getRegisterAttribute?.Invoke(type);
                    if (a == null)
                        continue;

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
                            services.Add(new ServiceDescriptor(type, type, a.Type));
                        }
                    }
                }
                catch { }
            }

        }

    }
}
