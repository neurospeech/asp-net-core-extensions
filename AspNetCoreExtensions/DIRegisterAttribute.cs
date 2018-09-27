using Microsoft.Extensions.DependencyInjection;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Microsoft.AspNetCore.Mvc
{
    [System.AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, Inherited = true, AllowMultiple = false)]
    [ExcludeFromCodeCoverage]
    public sealed class DIRegisterAttribute : Attribute
    {
        // public static List<DIRegisterAttribute> Registrations => new List<DIRegisterAttribute>();

        public ServiceLifetime Type { get; }

        public DIRegisterAttribute(ServiceLifetime type)
        {
            this.Type = type;

            // Registrations.Add(this);
        }

        /// <summary>
        /// 
        /// </summary>
        public Type BaseType { get; set; }

        public Type Factory { get; set; }

    }
}
