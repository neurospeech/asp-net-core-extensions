using Microsoft.Extensions.DependencyInjection;
using System;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace NeuroSpeech
{
    public interface IVersionProvider
    {

        Task<string> GetVersionAsync(PackagePathSegments path);

    }

    public static class NodePackageServiceExtensions
    {

        public static void AddNodePackageService(this IServiceCollection services,
            NodePackageServiceOptions options)
        {
            services.AddSingleton(sp => new NodePackageService(sp, options, null));
        }

        public static void AddNodePackageService<T>(this IServiceCollection services,
            NodePackageServiceOptions options)
            where T: IVersionProvider
        {
            services.AddSingleton(sp => new NodePackageService(sp, options, (s, path) => {
                var st = s.GetRequiredService<T>();
                return st.GetVersionAsync(path);
            }));
        }

        //public static IApplicationBuilder UseNpmDistribution(
        //    this IApplicationBuilder app,
        //    string route = "js-pkg/",
        //    bool crossBrowser = false)
        //{
        //    app.Map(route, a =>
        //    {
        //        a.Use(async (context, next) =>
        //        {

        //            HttpRequest request = context.Request;
        //            if (!request.Method.EqualsIgnoreCase("GET"))
        //            {
        //                await next();
        //                return;
        //            }

        //            PathString path = request.Path;

        //            IHeaderDictionary headers = context.Response.Headers;
        //            if (crossBrowser)
        //            {
        //                headers.Add("access-control-allow-origin", "*");
        //                headers.Add("access-control-expose-headers", "*");
        //                headers.Add("access-control-allow-methods", "*");
        //                headers.Add("access-control-allow-headers", "*");
        //                headers.Add("access-control-max-age", TimeSpan.FromDays(30).TotalSeconds.ToString());
        //            }

        //            var nodeServer = context.RequestServices.GetService<NodePackageService>();

        //            string sp = path.Value.Substring(route.Length + 1);

        //            var packageSegment = sp.ParseNPMPath();

        //            var package = await nodeServer.GetInstalledPackageAsync(sp);

        //            string folder = package.Path.TagFolder;

        //            string filePath = $"{folder}\\{packageSegment.Path}".Replace("/","\\");

        //            string host = context.Request.Query["host"];

        //            string contentType = MimeKit.MimeTypes.GetMimeType(filePath);

        //            var ct = MediaTypeHeaderValue.Parse(contentType);
        //            ct.CharSet = System.Text.Encoding.UTF8.EncodingName;
        //            context.Response.Headers.SetCommaSeparatedValues("content-type", ct.ToString());

        //            // await context.Response.SendFileAsync(filePath, context.RequestAborted);

        //            if (!string.IsNullOrWhiteSpace(host))
        //            {
        //                string lang = context.Request.Query["lang"];
        //                if (string.IsNullOrWhiteSpace(lang))
        //                {
        //                    lang = "en-US";
        //                }
        //                string designMode = context.Request.Query["designMode"];
        //                bool design = designMode.EqualsIgnoreCase("true");
        //                var startScript = $"UMD.lang = \"{lang}\";\r\n" +
        //                    $"UMD.hostView(\"{host}\",\"{packageSegment.Package}/{path}\", {(design? "true" : "false")})";
        //                await context.Response.WriteAsync(startScript);
        //            }
        //        });

        //    });

        //    return app;
        //}
    }
}
