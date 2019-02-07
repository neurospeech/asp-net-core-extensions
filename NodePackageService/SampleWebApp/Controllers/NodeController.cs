using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;


namespace SampleWebApp.Controllers
{

    [Route("n-api/{*path}")]
    public class NodeController: Controller
    {

        [HttpGet]
        [HttpPost]
        [HttpPut]
        [HttpDelete]
        public async Task<IActionResult> Run(
            [FromRoute] string path,
            [FromServices] NodeServer.NodeServer nodeServer)
        {

            path = $"@web-atoms/asp-net-core-node-server-test/{path}";

            var p = await nodeServer.GetInstalledPackageAsync(path);
            string body = null;
            if (Request.ContentLength > 0)
            {
                using(var reader = new StreamReader(Request.Body, System.Text.Encoding.UTF8))
                {
                    body = await reader.ReadToEndAsync();
                }
            }
            var q = new JObject();
            foreach(var item in Request.Query)
            {
                string s = string.Join(" ",item.Value);
                q.Add(item.Key, JValue.CreateString(s));
            }
            var result = await p.NodeServices.InvokeExportAsync<string>(
                "dist/index",
                "default",
                new MethodRequest {
                    Path = p.Path.Path,
                    Method = Request.Method,
                    Body = body,
                    BodyType = Request.ContentType,
                    Query = q
                });
            return Content(result, "application/json");
        }


        public class Result
        {
            [JsonProperty("success")]
            public bool Success { get; set; }

            [JsonProperty("content")]
            public string Content { get; set; }
        }

        public class MethodRequest
        {
            [JsonProperty("path")]
            public string Path { get; set; }

            [JsonProperty("method")]
            public string Method { get; set; }

            [JsonProperty("body")]
            public string Body { get; set; }

            [JsonProperty("bodyType")]
            public string BodyType { get; set; }

            [JsonProperty("query")]
            public JObject Query { get; set; }
        }
    }
}
