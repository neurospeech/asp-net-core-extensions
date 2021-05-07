using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace NeuroSpeech.Workflows
{
    public static class ServiceProviderExtensions
    {
        public static T Build<T>(this IServiceProvider services)
            => (T)Build(services, typeof(T));

        public static object Build(this IServiceProvider services, Type type) {
            var c = type.GetConstructors()[0];
            var cp = c.GetParameters();
            var args = new object[cp.Length];
            for (int i = 0; i < cp.Length; i++)
            {
                var t = cp[i].ParameterType;
                args[i] = services.GetRequiredService(t);
            }
            return c.Invoke(args);
        }

    }
}
