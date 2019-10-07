using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection;

namespace RetroCoreFit
{
    public interface IApiResponse
    {
        void Initialize(HttpResponseMessage response, object model);

        Type GetModelType();
    }

    public class ApiResponse<T> : IApiResponse
    {
        public Dictionary<string, string> Headers { get; protected set; }

        public T Model { get; protected set; }

        public virtual void Initialize(HttpResponseMessage response, object model)
        {

            this.Headers = new Dictionary<string, string>();

            var headerProperties = this.GetType().GetProperties().Select(x => new
            {
                Property = x,
                Header = x.GetCustomAttribute<HeaderAttribute>()
            }).Where(x => x.Header != null)
            .ToDictionary(x => x.Header.Name.ToLower(), x => x.Property);

            foreach (var k in response.Headers)
            {
                this.Headers[k.Key] = k.Value.ToString();
                if (headerProperties.TryGetValue(k.Key.ToLower(), out var p))
                {
                    object v = k.Value.ToString();
                    if (p.PropertyType != typeof(string))
                    {
                        var t = Nullable.GetUnderlyingType(p.PropertyType) ?? p.PropertyType;
                        v = Convert.ChangeType(v, t);
                    }
                    p.SetValue(this, v);
                }
            }

            this.Model = (T)model;
        }

        Type IApiResponse.GetModelType()
        {
            return typeof(T);
        }

    }
}
