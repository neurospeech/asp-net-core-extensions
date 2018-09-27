using System;
using System.Linq;

namespace Microsoft.AspNetCore.Mvc
{
    /// <summary>
    /// 
    /// </summary>
    public class HttpStatusException : Exception
    {

        /// <summary>
        /// 
        /// </summary>
        /// <param name="code"></param>
        /// <param name="message"></param>
        public HttpStatusException(int code, string message) : base(message)
        {
            this.Code = code;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="code"></param>
        /// <param name="message"></param>
        /// <param name="inner"></param>
        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
        public HttpStatusException(int code, string message, Exception inner) : base(message, inner)
        {
            this.Code = code;
        }

        /// <summary>
        /// 
        /// </summary>
        public int Code { get; private set; }
    }
}
