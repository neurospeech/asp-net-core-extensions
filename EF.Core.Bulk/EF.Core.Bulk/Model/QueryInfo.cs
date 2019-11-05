using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Microsoft.EntityFrameworkCore.Storage;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EFCoreBulk
{
    public class QueryInfo
    {
        public string Command { get; set; }
        public SelectExpression Sql { get; set; }
        public IReadOnlyDictionary<string, object> ParameterValues { get; set; }

    }
}
