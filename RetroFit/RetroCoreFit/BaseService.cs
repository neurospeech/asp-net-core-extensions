using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace RetroCoreFit
{
    public class BaseService {

        public Uri BaseUrl { get; set; }

        internal HttpClient client;

        internal Type interfaceType;

        internal Dictionary<string, RestCall> Methods;

        internal RestParameter[] Headers;

        public BaseService()
        {           

            
        }

        public Task<T> Invoke<T>(string key, params object[] plist) {


            var atlist = Methods[key];

            var rlist = atlist.Attributes.Select((x, i) => new RestParameter {
                Type = x,
                Value = plist[i]
            });

            return InvokeAsync<T>(atlist.Method, atlist.Path, rlist);
        }

        protected virtual async Task<T> InvokeAsync<T>(HttpMethod method, string path, IEnumerable<RestParameter> plist)
        {
            HttpContent content = null;

            Dictionary<string, string> headers = null;
            Dictionary<string, string> cookies = null;

            if (Headers != null && Headers.Any()) {
                headers = headers ?? new Dictionary<string, string>();
                foreach (var header in this.Headers) {
                    headers[(header.Type as HeaderAttribute).Name] = 
                        (header.Value as PropertyInfo).GetValue(this)?.ToString();
                }
            }

            foreach (var rp in plist) {

                switch (rp.Type) {


                    case BodyAttribute b:
                        content = EncodePost(rp.Value);
                        break;
                    case QueryAttribute q:

                        if (rp.Value == null)
                            continue;

                        if (!path.Contains("?")) {
                            path += "?";
                        }

                        path += $"{q.Name}={Uri.EscapeDataString(rp.Value.ToString())}&"; 

                        break;

                    case PathAttribute p:
                        path = path.Replace("{" + p.Name + "}", rp.Value.ToString());
                        break;

                    case HeaderAttribute h:
                        headers = headers ?? new Dictionary<string, string>();
                        headers[h.Name] = rp.Value.ToString();
                        break;

                    case CookieAttribute c:
                        cookies = cookies ?? new Dictionary<string, string>();
                        cookies[c.Name] = rp.Value.ToString();
                        break;

                    case MultipartAttribute ma:
                        MultipartFormDataContent mfd = (content as MultipartFormDataContent) ?? ((MultipartFormDataContent)(content = new MultipartFormDataContent()));

                        switch (rp.Value) {
                            case string s:
                                mfd.Add(new StringContent(s), ma.Name);
                                break;
                            case byte[] d:
                                mfd.Add(new ByteArrayContent(d), ma.Name);
                                break;
                            case Stream s:
                                mfd.Add(new StreamContent(s), ma.Name);
                                break;
                            default:
                                throw new NotSupportedException();
                        }
                        break;

                    case MultipartFileAttribute ma:
                        mfd = (content as MultipartFormDataContent) ?? ((MultipartFormDataContent)(content = new MultipartFormDataContent()));

                        switch (rp.Value)
                        {
                            case string s:
                                mfd.Add(new StringContent(s), ma.Name, ma.FileName);
                                break;
                            case byte[] d:
                                mfd.Add(new ByteArrayContent(d), ma.Name, ma.FileName);
                                break;
                            case Stream s:
                                mfd.Add(new StreamContent(s), ma.Name, ma.FileName);
                                break;
                            default:
                                throw new NotSupportedException();
                        }
                        break;

                }

            }

            if (BaseUrl != null)
            {
                Uri uri = new Uri(BaseUrl, path);
                path = uri.ToString();
            }

            HttpRequestMessage request = new HttpRequestMessage(method, path);

            if (content != null) {
                request.Content = content;
            }

            if (headers != null) {
                foreach (var h in headers) {
                    if (h.Value == null)
                        continue;
                    request.Headers.TryAddWithoutValidation(h.Key, h.Value);
                }
            }

            if (cookies != null) {
                var c = string.Join(";", cookies.Select(x => $"{x.Key}={x.Value}"));
                request.Headers.TryAddWithoutValidation("Cookie", c);
            }

            var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

            Type returnType = typeof(T);

            if (returnType == typeof(HttpResponseMessage))
            {
                return (T)(object)response;
            }

            if (response.IsSuccessStatusCode) {

                if (returnType == typeof(byte[])) {
                    return (T)(object) await response.Content.ReadAsByteArrayAsync();
                }
                if (returnType == typeof(Stream))
                {
                    return (T)(object)await response.Content.ReadAsStreamAsync();
                }
                if (returnType == typeof(String))
                {
                    return (T)(object)await response.Content.ReadAsStringAsync();
                }

                string text = await response.Content.ReadAsStringAsync();

                return DecodeResult<T>(text);
            }

            string error = await response.Content.ReadAsStringAsync();

            throw new HttpRequestException(path + "\r\n" + error);
        }

        protected virtual T DecodeResult<T>(string text)
        {
            return JsonConvert.DeserializeObject<T>(text);
        }

        protected virtual HttpContent EncodePost(object value)
        {
            HttpContent content = new StringContent(JsonConvert.SerializeObject(value), Encoding.UTF8, "application/json");
            return content;
        }
    }
}
