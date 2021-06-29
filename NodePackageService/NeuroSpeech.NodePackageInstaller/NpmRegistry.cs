using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace NeuroSpeech
{
    public class NpmRegistry
    {
        private readonly HttpClient client;

        public NpmRegistry(HttpClient client)
        {
            this.client = client;
        }

        public async Task<string> GetTarBallAsync(string url, string version)
        {
            try {
                return await GetTarBallForAsync(url + "/" + version, version);
            }
            catch { }
            return await GetTarBallForAsync(url, version);
        }

        private async Task<string> GetTarBallForAsync(string url, string v)
        {
            v = v.Trim('v', 'V');
            var json = await client.GetStringAsync(url);
            var package = JObject.Parse(json);
            if(package.TryGetValue("dist", out var token))
            {
                var dist = token as JObject;
                return dist.GetValue("tarball").ToString();
            }

            if(package.TryGetValue("versions", out token))
            {
                var versions = token as JObject;
                if(versions.TryGetValue(v, out token))
                {
                    var dist = token as JObject;
                    return dist.GetValue("tarball").ToString();
                }
            }
            throw new KeyNotFoundException($"Version {v} not found in {json}");
        }
    }
}
