using Microsoft.Extensions.DependencyInjection;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Microsoft.AspNetCore.Mvc
{
    [ExcludeFromCodeCoverage]
    public abstract class BaseDIFactory
    {
        public abstract object CreateService(IServiceProvider sp);
    }

    [ExcludeFromCodeCoverage]
    public abstract class DIFactory<T> : BaseDIFactory
    {
        public abstract T Create(IServiceProvider sp);

        public override object CreateService(IServiceProvider sp)
        {
            return Create(sp);
        }
    }

}
