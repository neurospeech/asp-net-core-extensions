using Microsoft.EntityFrameworkCore.Query.Expressions;
using Microsoft.EntityFrameworkCore.Storage;
using System;
using System.Linq;

namespace EF.Core.Bulk
{
    public class QueryInfo
    {
        public IRelationalCommand Command { get; set; }
        public SelectExpression Sql { get; set; }
        public Parameters ParameterValues { get; set; }

    }
}
