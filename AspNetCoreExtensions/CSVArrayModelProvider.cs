using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Mvc
{

    /// <summary>
    /// Excluded by Akash Kava
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class CSVArrayModelProvider : IModelBinderProvider
    {
        private static Dictionary<Type, IModelBinder> binders = new Dictionary<Type, IModelBinder>();

        public IModelBinder GetBinder(ModelBinderProviderContext context)
        {
            var type = context.Metadata.ModelType;
            if (!type.IsArray)
                return null;
            IModelBinder binder;
            if (binders.TryGetValue(type, out binder))
                return binder;
            binder = new CSVArrayModelBinder(type, context.Services.GetService<ILoggerFactory>());
            binders[type] = binder;
            return binder;
        }
    }
}
