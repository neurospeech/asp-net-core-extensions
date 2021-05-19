using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Microsoft.EntityFrameworkCore.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace EFCoreBulk.Model
{
    internal static class ExpHelper
    {
        internal static T VisitNode<T>(this T exp, ExpressionVisitor visitor)
            where T: Expression
        {
            if (exp == null)
                return null;
            return visitor.Visit(exp) as T;
        }

        internal static List<T> VisitAll<T>(this IEnumerable<T> list, ExpressionVisitor visitor)
            where T : Expression
        {
            if (list == null)
            {
                return null;
            }
            return list.Select(x => visitor.Visit(x) as T).ToList();
        }


    }

    public class FixCastErrorExpressionVisitor: SqlExpressionVisitor
    {
        private RelationalQueryContext queryContext;
        readonly SqlExpressionFactory sqlFactory;

        public FixCastErrorExpressionVisitor(
            QuerySqlGeneratorDependencies dependencies, 
            RelationalQueryContext queryContext,
            SqlExpressionFactory factory)
        {
            this.sqlFactory = factory;
            this.queryContext = queryContext;
        }

        protected override Expression VisitIn(InExpression inExpression)
        {
            if (inExpression.Values is SqlParameterExpression spe)
            {
                var plist = queryContext.ParameterValues[spe.Name] as System.Collections.IList;

                // inExpression.Item

                // sqlFactory.a

                // var ce = sqlFactory.Constant(plist.Select(x => (object));

                List<object> list = new List<object>();
                foreach(var item in plist)
                {
                    list.Add(item);
                }

                var ce = sqlFactory.Constant(list, sqlFactory.GetTypeMappingForValue(list.FirstOrDefault()));

                // sqlFactory.MakeBinary(ExpressionType.or)



                return inExpression.Update(inExpression.Item, ce, inExpression.Subquery);

                //var _relationalCommandBuilder = this.GetPrivateField<IRelationalCommandBuilder>("_relationalCommandBuilder");
                //_relationalCommandBuilder.Append(inExpression.IsNegated ? " NOT IN " : " IN ");
                //_relationalCommandBuilder.Append("(");
                //// _relationalCommandBuilder.Append(spe.Name);

                //// we need to expand our list here...
                //var prefix = spe.Name;

                //var plist = queryContext.ParameterValues[spe.Name] as System.Collections.IList;
                //int i = 0;
                //foreach(var pv in plist)
                //{
                //    var n = $"{prefix}_{i}";
                //    queryContext.AddParameter(n, pv);
                //    if (i != 0)
                //    {
                //        _relationalCommandBuilder.Append(", ");
                //    }
                //    _relationalCommandBuilder.AppendLine($"@{n}");
                //    i++;
                //}

                //_relationalCommandBuilder.Append(")");
                // return inExpression;
            }

            return inExpression;
        }

        public SelectExpression FixError(SelectExpression selectExpression)
        {
            if (selectExpression == null || selectExpression.Predicate == null)
            {
                return selectExpression;
            }
            var p =  (this.VisitExtension(selectExpression.Predicate) ?? selectExpression.Predicate) as SqlExpression;
            return selectExpression.Update(
                selectExpression.Projection.ToList(),
                selectExpression.Tables.ToList(),
                p,
                selectExpression.GroupBy.ToList(),
                selectExpression.Having,
                selectExpression.Orderings.ToList(),
                selectExpression.Limit,
                selectExpression.Offset,
                selectExpression.IsDistinct,
                selectExpression.Alias
                );
        }

        protected override Expression VisitRowNumber(RowNumberExpression rowNumberExpression)
        {
            return rowNumberExpression?.Update(
                rowNumberExpression.Partitions.VisitAll(this),
                rowNumberExpression.Orderings.VisitAll(this));
        }

        protected override Expression VisitExcept(ExceptExpression exceptExpression)
        {
            return exceptExpression?.Update(
                exceptExpression.Source1.VisitNode(this),
                exceptExpression.Source2.VisitNode(this)
                );
        }

        protected override Expression VisitIntersect(IntersectExpression x)
        {
            return x?.Update(x.Source1.VisitNode(this), x.Source2.VisitNode(this));
        }

        protected override Expression VisitUnion(UnionExpression x)
        {
            return x?.Update(x.Source1.VisitNode(this), x.Source2.VisitNode(this));
        }

        protected override Expression VisitExists(ExistsExpression x)
        {
            return x?.Update(x.Subquery.VisitNode(this));
        }

        protected override Expression VisitCrossJoin(CrossJoinExpression x)
        {
            return x?.Update(x.Table.VisitNode(this));
        }

        protected override Expression VisitCrossApply(CrossApplyExpression x)
        {
            return x?.Update(x.Table.VisitNode(this));
        }

        protected override Expression VisitOuterApply(OuterApplyExpression x)
        {
            return x?.Update(x.Table.VisitNode(this));
        }

        protected override Expression VisitFromSql(FromSqlExpression x)
        {
            return x;
        }

        protected override Expression VisitInnerJoin(InnerJoinExpression x)
        {
            return x?.Update(x.Table.VisitNode(this), x.JoinPredicate.VisitNode(this));
        }

        protected override Expression VisitLeftJoin(LeftJoinExpression x)
        {
            return x?.Update(x.Table.VisitNode(this), x.JoinPredicate.VisitNode(this));
        }

        protected override Expression VisitProjection(ProjectionExpression x)
        {
            return x?.Update(x.Expression.VisitNode(this));
        }

        protected override Expression VisitCase(CaseExpression x)
        {
            return x?.Update(x.Operand.VisitNode(this), x.WhenClauses.ToList(), x.ElseResult.VisitNode(this));
        }

        protected override Expression VisitSqlUnary(SqlUnaryExpression x)
        {
            return x?.Update(x.Operand.VisitNode(this));
        }

        protected override Expression VisitSqlFunction(SqlFunctionExpression x)
        {
            return x?.Update(x.Instance.VisitNode(this) as SqlExpression, x.Arguments.VisitAll(this));
        }

        protected override Expression VisitSqlFragment(SqlFragmentExpression x)
        {
            return x;
        }

        protected override Expression VisitOrdering(OrderingExpression x)
        {
            return x?.Update(x.Expression.VisitNode(this));
        }

        protected override Expression VisitSqlParameter(SqlParameterExpression x)
        {
            return x;
        }

        protected override Expression VisitSqlBinary(SqlBinaryExpression x)
        {
            return x?.Update(x.Left.VisitNode(this), x.Right.VisitNode(this));
        }

        protected override Expression VisitColumn(ColumnExpression x)
        {
            return x;
        }

        protected override Expression VisitSelect(SelectExpression x)
        {
            return x?.Update(
                x.Projection.VisitAll(this),
                x.Tables.VisitAll(this),
                x.Predicate.VisitNode(this),
                x.GroupBy.VisitAll(this),
                x.Having.VisitNode(this),
                x.Orderings.VisitAll(this),
                x.Limit.VisitNode(this),
                x.Offset.VisitNode(this),
                x.IsDistinct,
                x.Alias);
        }

        protected override Expression VisitTable(TableExpression x)
        {
            return x;
        }

        protected override Expression VisitSqlConstant(SqlConstantExpression x)
        {
            return x;
        }

        protected override Expression VisitLike(LikeExpression x)
        {
            return x?.Update(x.Match.VisitNode(this), x.Pattern.VisitNode(this), x.EscapeChar.VisitNode(this));
        }

        protected override Expression VisitSubSelect(ScalarSubqueryExpression x)
        {
            return x?.Update(x.Subquery.VisitNode(this));
        }

    }
}
