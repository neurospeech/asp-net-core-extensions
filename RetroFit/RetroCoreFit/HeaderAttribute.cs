using System;
using System.Net.Http;

namespace RetroCoreFit
{

    public abstract class RestAttribute : Attribute { }

    public class NamedAttribute : RestAttribute {

        public NamedAttribute(string name)
        {
            this.Name = name;
        }

        public string Name { get; internal set; }

    }

    [System.AttributeUsage(AttributeTargets.Property | AttributeTargets.Method | AttributeTargets.Parameter, Inherited = false, AllowMultiple = true)]
    public sealed class HeaderAttribute : NamedAttribute
    {
        public HeaderAttribute(string name) : base(name)
        {
        }
    }

    //[System.AttributeUsage(AttributeTargets.Interface, Inherited = false, AllowMultiple = true)]
    //public sealed class BaseUrlAttribute : NamedAttribute {
    //    public BaseUrlAttribute(string name) : base(name)
    //    {
    //    }
    //}

    public abstract class HttpMethodAttribute : NamedAttribute
    {
        public HttpMethodAttribute(HttpMethod method, string path) : base(path)
        {
            this.Method = method;
        }

        public HttpMethod Method { get; }
        
    }


    [System.AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = true)]
    public sealed class PutAttribute : HttpMethodAttribute
    {
        public PutAttribute(string name) : base(HttpMethod.Put, name)
        {
        }
    }

    [System.AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = true)]
    public sealed class PostAttribute : HttpMethodAttribute
    {
        public PostAttribute(string name) : base(HttpMethod.Post, name)
        {
        }
    }

    [System.AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = true)]
    public sealed class GetAttribute : HttpMethodAttribute
    {
        public GetAttribute(string name) : base(HttpMethod.Get, name)
        {
        }
    }

    [System.AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = true)]
    public sealed class DeleteAttribute : HttpMethodAttribute
    {
        public DeleteAttribute(string name) : base(HttpMethod.Delete, name)
        {
        }
    }

    [System.AttributeUsage(AttributeTargets.Parameter, Inherited = false, AllowMultiple = true)]
    public sealed class BodyAttribute : RestAttribute {
    }


    public class ParamAttribute : RestAttribute {
        public ParamAttribute()
        {

        }

        public ParamAttribute(string name)
        {
            this.Name = name;
        }

        public string Name { get; set; }
    }


    [AttributeUsage(AttributeTargets.Parameter)]
    public sealed class QueryAttribute : ParamAttribute {
        public QueryAttribute()
        {

        }

        public QueryAttribute(string name):base(name)
        {

        }
    }


    [AttributeUsage(AttributeTargets.Parameter)]
    public sealed class PathAttribute : ParamAttribute
    {
        public PathAttribute()
        {

        }

        public PathAttribute(string name) : base(name)
        {

        }
    }

    [AttributeUsage(AttributeTargets.Parameter)]
    public sealed class CookieAttribute : ParamAttribute
    {
        public CookieAttribute()
        {

        }

        public CookieAttribute(string name) : base(name)
        {

        }
    }

    [AttributeUsage(AttributeTargets.Parameter)]
    public sealed class MultipartAttribute : ParamAttribute {
        public MultipartAttribute()
        {

        }

        public MultipartAttribute(string name): base(name)
        {

        }

    }

    [AttributeUsage(AttributeTargets.Parameter)]
    public sealed class MultipartFileAttribute : ParamAttribute
    {
        public MultipartFileAttribute()
        {

        }

        public MultipartFileAttribute(string name) : base(name)
        {

        }

        public string FileName { get; set; }
    }

    [AttributeUsage(AttributeTargets.Parameter)]
    public sealed class FormAttribute : ParamAttribute
    {
        public FormAttribute(string name) : base(name)
        {

        }
    }
}
