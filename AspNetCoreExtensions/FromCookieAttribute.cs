using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Mvc
{
    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class FromCookieAttribute : Attribute, IBindingSourceMetadata, IModelNameProvider
    {
        public BindingSource BindingSource => CookieBindingSource.Instance;

        public string Name { get; set; }
    }

    public static class CookieBindingSource
    {
        public static readonly BindingSource Instance = new BindingSource(
            "Cookie",
            "Cookie",
            isGreedy: false,
            isFromRequest: true);
    }

    public class CookieValueProviderFactory : IValueProviderFactory
    {
        public Task CreateValueProviderAsync(ValueProviderFactoryContext context)
        {
            var cookies = context.ActionContext.HttpContext.Request.Cookies;

            context.ValueProviders.Add(new CookieValueProvider(CookieBindingSource.Instance, cookies));

            return Task.CompletedTask;
        }

        Task IValueProviderFactory.CreateValueProviderAsync(ValueProviderFactoryContext context)
        {
            throw new NotImplementedException();
        }
    }

    public class CookieValueProvider : BindingSourceValueProvider
    {
        public CookieValueProvider(BindingSource bindingSource, IRequestCookieCollection cookies) : base(bindingSource)
        {
            Cookies = cookies;
        }

        private IRequestCookieCollection Cookies { get; }

        public override bool ContainsPrefix(string prefix)
        {
            return Cookies.ContainsKey(prefix);
        }

        public override ValueProviderResult GetValue(string key)
        {
            return Cookies.TryGetValue(key, out var value) ? new ValueProviderResult(value) : ValueProviderResult.None;
        }
    }
}
