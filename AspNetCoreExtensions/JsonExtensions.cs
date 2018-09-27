using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Microsoft.AspNetCore.Mvc
{
    /// <summary>
    /// 
    /// </summary>
    public static class JsonExtensions
    {

        /// <summary>
        /// 
        /// </summary>
        /// <remarks>
        /// As method simply converts key value pair in different form, no code coverage is needed.
        /// </remarks>
        /// <typeparam name="T"></typeparam>
        /// <param name="jObject"></param>
        /// <returns></returns>
        [ExcludeFromCodeCoverage]
        public static Dictionary<string, T> ToDictionary<T>(this JObject jObject)
        {
            var dict = new Dictionary<string, T>();

            foreach (var p in jObject.Properties())
            {
                dict[p.Name] = p.Value.ToObject<T>();
            }
            return dict;
        }
    }
}
