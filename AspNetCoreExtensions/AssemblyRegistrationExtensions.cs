using Microsoft.Extensions.DependencyInjection;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;

namespace NeuroSpeech
{
    [ExcludeFromCodeCoverage]
    public static class AssemblyRegistrationExtensions
    {

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
