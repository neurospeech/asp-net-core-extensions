using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace RetroCoreFit
{
    public class ApiException : HttpException
    {
        public JToken Details { get; }

        public ApiException(
            string path,
            HttpStatusCode statusCode,
            string message,
            JToken details)
            : base(path, statusCode, message)
        {
            this.Details = details;
        }

        public override string ToString()
        {
            var error = $"Status: {StatusCode}, Error = {Message}\r\nUrl: {this.Path}\r\n{Details.ToString(Formatting.Indented)}\r\n{this.StackTrace}";
            return error;
        }
    }

    public class HttpException : Exception
    {
        public string Path { get; }

        public HttpStatusCode StatusCode { get; }
        public HttpException(
            string path,
            HttpStatusCode statusCode,
            string content)
            : base(content)
        {
            this.Path = path;
            this.StatusCode = statusCode;
        }

        public override string ToString()
        {
            var error = $"Status: {StatusCode}, Error = {Message}\r\nUrl: {this.Path}\r\n{this.StackTrace}";
            return error;
        }
    }

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
            Dictionary<string, string> form = null;

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
                        if (h.Name != null)
                        {
                            headers[h.Name] = rp.Value.ToString();
                        }
                        else if (rp.Value is KeyValuePair<string, string> kvp)
                        {
                            headers[kvp.Key] = kvp.Value;
                        }
                        else if (rp.Value is IEnumerable<KeyValuePair<string, string>> kvps)
                        {
                            foreach (var item in kvps)
                            {
                                headers.Add(item.Key, item.Value);
                            }
                        }
                        break;

                    case CookieAttribute c:
                        cookies = cookies ?? new Dictionary<string, string>();
                        if (c.Name != null)
                        {
                            cookies[c.Name] = rp.Value.ToString();
                        }
                        else if (rp.Value is KeyValuePair<string, string> kvp)
                        {
                            cookies[kvp.Key] = kvp.Value;
                        }
                        else if (rp.Value is IEnumerable<KeyValuePair<string, string>> kvps)
                        {
                            foreach (var item in kvps)
                            {
                                cookies.Add(item.Key, item.Value);
                            }
                        }
                        break;

                    case FormAttribute f:
                        form = form ?? new Dictionary<string, string>();
                        form[f.Name] = rp.Value.ToString();
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
                if (!(path.StartsWith("https://") && path.StartsWith("http://"))) {
                    Uri uri = new Uri(BaseUrl, path);
                    path = uri.ToString();
                }
            }

            HttpRequestMessage request = new HttpRequestMessage(method, path);

            if (content != null) {
                request.Content = content;
            }

            if (form != null)
            {
                content = new FormUrlEncodedContent(form);
            }

            if (content != null)
            {
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


                if (typeof(IApiResponse).IsAssignableFrom(returnType))
                {
                    var rv = (object)Activator.CreateInstance<T>();
                    var irv = rv as IApiResponse;
                    var r = await DecodeResultAsync(response.Content, irv.GetModelType());
                    irv.Initialize(response, r);
                    return (T)(object)rv;
                }

                return (T)( await DecodeResultAsync(response.Content, returnType));

            }

            string error = await response.Content.ReadAsStringAsync();
            if(response.Content.Headers.ContentType?.ToString()?.Contains("/json") ?? false)
            {
                var e = JToken.Parse(error);
                var message = "Application Error";
                if (e is JObject)
                {
                    var msg = e.OfType<JProperty>().FirstOrDefault(x =>
                        x.Name.Equals("exceptionMessage", StringComparison.OrdinalIgnoreCase)
                        || x.Name.Equals("message", StringComparison.OrdinalIgnoreCase)
                        || x.Name.Equals("error", StringComparison.OrdinalIgnoreCase)
                        || x.Name.Equals("errors", StringComparison.OrdinalIgnoreCase));

                    if (!(msg.Value is JArray))
                    {
                        message = msg.Value.ToString();
                    }
                }
                throw new ApiException(
                    path,
                    response.StatusCode,
                    message,
                    e);
            }
            throw new HttpException(path, response.StatusCode, error);
        }

        protected virtual async Task<object> DecodeResultAsync(HttpContent content, Type returnType)
        {
            if (returnType == typeof(byte[]))
            {
                return await content.ReadAsByteArrayAsync();
            }
            if (returnType == typeof(Stream))
            {
                return await content.ReadAsStreamAsync();
            }
            if (returnType == typeof(String))
            {
                return await content.ReadAsStringAsync();
            }

            string text = await content.ReadAsStringAsync();

            if (returnType == typeof(JObject))
            {
                return JObject.Parse(text);
            }

            if (returnType == typeof(JArray))
            {
                return JArray.Parse(text);
            }
            return JsonConvert.DeserializeObject(text, returnType);
        }

        protected virtual HttpContent EncodePost(object value)
        {
            HttpContent content = new StringContent(JsonConvert.SerializeObject(value), Encoding.UTF8, "application/json");
            return content;
        }
    }
}
