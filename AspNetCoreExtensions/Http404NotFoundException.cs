using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Microsoft.AspNetCore.Mvc
{
    /// <summary>
    /// 
    /// </summary>
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public class Http404NotFoundException : HttpStatusException
    {
        /// <summary>
        /// </summary>
        /// <param name="code"></param>
        /// <param name="message"></param>
        public Http404NotFoundException(string message) : base(404, message)
        {

        }

        /// <summary>
        /// </summary>
        /// <param name="code"></param>
        /// <param name="message"></param>
        /// <param name="inner"></param>
        [ExcludeFromCodeCoverage]
        public Http404NotFoundException(string message, Exception inner) : base(404, message, inner)
        {

        }
    }
}
