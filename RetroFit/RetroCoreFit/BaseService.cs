using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading;
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

        public BaseService(HttpClient client)
        {

            this.client = client;
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public Task<T> Invoke<T>(string key, params object[] plist) {


            var atlist = Methods[key];

            var rlist = atlist.Attributes.Select((x, i) => new RestParameter {
                Type = x,
                Value = plist[i]
            });

            return InvokeAsync<T>(atlist.Method, atlist.Path, rlist);
        }

        protected Task<T> PostAsync<T>(string path, params RestParameter[] args) =>
            InvokeAsync<T>(HttpMethod.Post, path, args);
        protected Task<T> GetAsync<T>(string path, params RestParameter[] args) =>
            InvokeAsync<T>(HttpMethod.Get, path, args);
        protected Task<T> DeleteAsync<T>(string path, params RestParameter[] args) =>
            InvokeAsync<T>(HttpMethod.Delete, path, args);
        protected Task<T> PutAsync<T>(string path, params RestParameter[] args) =>
            InvokeAsync<T>(HttpMethod.Put, path, args);

        protected virtual async Task<T> InvokeAsync<T>(HttpMethod method, string path, IEnumerable<RestParameter> plist)
        {
            HttpContent content = null;

            Dictionary<string, string> headers = null;
            Dictionary<string, string> cookies = null;
            Dictionary<string, string> form = null;
            CancellationToken token = CancellationToken.None;

            if (Headers != null && Headers.Any())
            {
                headers = headers ?? new Dictionary<string, string>();
                foreach (var header in this.Headers)
                {
                    headers[(header.Type as HeaderAttribute).Name] =
                        (header.Value as PropertyInfo).GetValue(this)?.ToString();
                }
            }

            foreach (var rp in plist)
            {

                object rpValue = rp.Value;

                if (rpValue != null)
                {
                    Type valueType = rpValue.GetType();
                    if (valueType.IsEnum)
                    {
                        valueType = Enum.GetUnderlyingType(valueType);
                        if (valueType != null)
                        {
                            rpValue = Convert.ChangeType(rpValue, valueType);
                        }
                    }
                }


                switch (rp.Type)
                {


                    case BodyAttribute b:
                        content = EncodePost(rpValue);
                        break;
                    case QueryAttribute q:

                        if (rpValue == null)
                            continue;

                        if (!path.Contains("?"))
                        {
                            path += "?";
                        }

                        path += $"{q.Name}={Uri.EscapeDataString(rpValue.ToString())}&";

                        break;

                    case PathAttribute p:
                        path = path.Replace("{" + p.Name + "}", rpValue.ToString());
                        break;

                    case HeaderAttribute h:
                        headers = headers ?? new Dictionary<string, string>();
                        if (h.Name != null)
                        {
                            headers[h.Name] = rpValue.ToString();
                        }
                        else if (rpValue is KeyValuePair<string, string> kvp)
                        {
                            headers[kvp.Key] = kvp.Value;
                        }
                        else if (rpValue is IEnumerable<KeyValuePair<string, string>> kvps)
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
                            cookies[c.Name] = rpValue.ToString();
                        }
                        else if (rpValue is KeyValuePair<string, string> kvp)
                        {
                            cookies[kvp.Key] = kvp.Value;
                        }
                        else if (rpValue is IEnumerable<KeyValuePair<string, string>> kvps)
                        {
                            foreach (var item in kvps)
                            {
                                cookies.Add(item.Key, item.Value);
                            }
                        }
                        break;

                    case FormAttribute f:
                        form = form ?? new Dictionary<string, string>();
                        form[f.Name] = rpValue.ToString();
                        break;

                    case MultipartAttribute ma:
                        MultipartFormDataContent mfd = (content as MultipartFormDataContent) ?? ((MultipartFormDataContent)(content = new MultipartFormDataContent()));

                        switch (rpValue)
                        {
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

                        switch (rpValue)
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
                    case CancelAttribute ca:
                        token = (CancellationToken)rpValue;
                        break;

                }

            }

            if (BaseUrl != null)
            {
                if (!(path.StartsWith("https://") && path.StartsWith("http://")))
                {
                    Uri uri = new Uri(BaseUrl, path);
                    path = uri.ToString();
                }
            }

            HttpRequestMessage request = new HttpRequestMessage(method, path);

            if (content != null)
            {
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

            if (headers != null)
            {
                foreach (var h in headers)
                {
                    if (h.Value == null)
                        continue;
                    request.Headers.TryAddWithoutValidation(h.Key, h.Value);
                }
            }

            if (cookies != null)
            {
                var c = string.Join(";", cookies.Select(x => $"{x.Key}={x.Value}"));
                request.Headers.TryAddWithoutValidation("Cookie", c);
            }
            HttpResponseMessage response = await SendRequestAsync(request, token);

            Type returnType = typeof(T);

            if (returnType == typeof(HttpResponseMessage))
            {
                return (T)(object)response;
            }

            if (returnType == typeof(Stream))
            {
                // return await content.ReadAsStreamAsync();
                throw new NotSupportedException("In order to read Stream, use HttpResponseMessage as return type of Method");
            }

            using (response) {

                if (response.IsSuccessStatusCode)
                {

                    if (typeof(IApiResponse).IsAssignableFrom(returnType))
                    {
                        var rv = (object)Activator.CreateInstance<T>();
                        var irv = rv as IApiResponse;
                        var r = await DecodeResultAsync(response.Content, irv.GetModelType(), token);
                        irv.Initialize(response, r);
                        if (token.IsCancellationRequested) throw new TaskCanceledException();
                        
                        return (T)(object)rv;
                    }

                    return (T)(await DecodeResultAsync(response.Content, returnType, token));

                }

                string error = await response.Content.ReadAsStringAsync();
                if (response.Content.Headers.ContentType?.ToString()?.Contains("/json") ?? false)
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
        }

        protected virtual async Task<HttpResponseMessage> SendRequestAsync(HttpRequestMessage request, CancellationToken token)
        {
            return await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token);
        }

        protected virtual async Task<object> DecodeResultAsync(HttpContent content, Type returnType, CancellationToken token)
        {

            if (returnType == typeof(byte[]))
            {
                return await content.ReadAsByteArrayAsync();
            }
            if (returnType == typeof(String))
            {
                return await content.ReadAsStringAsync();
            }

            string text = await content.ReadAsStringAsync();
            if (token.IsCancellationRequested) throw new TaskCanceledException();
            var r = DeserializeJson(text, returnType);
            return r;
        }


        /// <summary>
        /// Implementation must take care of JObject and JArray as well
        /// </summary>
        /// <param name="text"></param>
        /// <param name="returnType"></param>
        /// <returns></returns>
        protected virtual object DeserializeJson(string text, Type returnType)
        {
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
