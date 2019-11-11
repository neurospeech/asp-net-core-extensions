using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using System.Reflection;
using Newtonsoft.Json.Linq;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Mvc
{
    /// <summary>
    /// Excluded by Akash Kava
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class CSVArrayModelBinder : IModelBinder
    {
        private Type type;
        private SimpleTypeModelBinder fallbackBinder;

        public CSVArrayModelBinder(Type type, ILoggerFactory loggerFactory)
        {
            this.type = type;
            this.fallbackBinder = new SimpleTypeModelBinder(type, loggerFactory);
        }

        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            var name = bindingContext.ModelName;
            var value = bindingContext.ValueProvider.GetValue(name);
            if (value.Length == 0 || !bindingContext.IsTopLevelObject)
            {
                return fallbackBinder.BindModelAsync(bindingContext);
            }

            var v = value.FirstValue;
            var tokens = v.Split(",").Select(x => x.Trim()).Where(x => !string.IsNullOrWhiteSpace(x));

            var elementType = type.GetElementType();

            var list = new ArrayList();
            foreach (var token in tokens)
            {
                var id = Convert.ChangeType(token, elementType);
                list.Add(id);
            }
            Array a = list.ToArray(elementType);
            bindingContext.ModelState.SetModelValue(bindingContext.ModelName, a, v);
            bindingContext.Result = ModelBindingResult.Success(a);
            return Task.CompletedTask;
        }

        Task IModelBinder.BindModelAsync(ModelBindingContext bindingContext)
        {
            throw new NotImplementedException();
        }
    }
}
