using System;
using System.Net.Http;

namespace RetroCoreFit
{
    public struct RestCall {
        public string Path;
        public HttpMethod Method;
        public RestAttribute[] Attributes;
    }
}
