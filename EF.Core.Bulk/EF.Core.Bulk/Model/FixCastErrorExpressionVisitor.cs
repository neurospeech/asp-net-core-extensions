using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Microsoft.EntityFrameworkCore.Storage;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace EFCoreBulk.Model
{
    public class FixCastErrorExpressionVisitor: QuerySqlGenerator
    {
        public FixCastErrorExpressionVisitor(QuerySqlGeneratorDependencies dependencies) :
            base(dependencies)
        {

        }

        protected override Expression VisitIn(InExpression inExpression)
        {

            if (inExpression.Values is SqlParameterExpression spe)
            {
                var _relationalCommandBuilder = this.GetPrivateField<IRelationalCommandBuilder>("_relationalCommandBuilder");
                _relationalCommandBuilder.Append(inExpression.IsNegated ? " NOT IN " : " IN ");
                _relationalCommandBuilder.Append("(");
                _relationalCommandBuilder.Append(spe.Name);
                _relationalCommandBuilder.Append(")");
                return inExpression;
            }

            return base.VisitIn(inExpression);
        }

    }
}
