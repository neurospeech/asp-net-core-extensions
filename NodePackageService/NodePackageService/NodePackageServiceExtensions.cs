using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace NeuroSpeech
{
    public static class NodePackageServiceExtensions
    {

        public static void AddNodePackageService(this IServiceCollection services,
            NodePackageServiceOptions options)
        {
            services.AddSingleton(sp => new NodePackageService(sp, options));
        }

        public static IApplicationBuilder UseUIViews(this IApplicationBuilder app, string route = "uiv/")
        {

            app.Use(async (context, next) =>
            {

                HttpRequest request = context.Request;
                if (!request.Method.EqualsIgnoreCase("GET"))
                {
                    await next();
                    return;
                }

                PathString path = request.Path;
                if (!path.HasValue || !path.Value.StartsWithIgnoreCase(route))
                {
                    await next();
                    return;
                }

                IHeaderDictionary headers = context.Response.Headers;
                headers.Add("access-control-allow-origin", "*");
                headers.Add("access-control-expose-headers", "*");
                headers.Add("access-control-allow-methods", "*");
                headers.Add("access-control-allow-headers", "*");
                headers.Add("access-control-max-age", TimeSpan.FromDays(30).TotalSeconds.ToString());

                var nodeServer = context.RequestServices.GetService<NodePackageService>();

                string sp = path.Value.Substring(4);

                PackagePath packagePath = nodeServer.ParsePath(sp);

                await nodeServer.DownloadAsync(packagePath);
                
                // get file content...
            });

            return app;
        }

    }
}
