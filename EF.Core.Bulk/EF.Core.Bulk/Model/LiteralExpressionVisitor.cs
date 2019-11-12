using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Microsoft.EntityFrameworkCore.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace EFCoreBulk
{
    internal class LiteralExpressionVisitor : ExpressionVisitor
    {


        public IEnumerable<(PropertyInfo,object)> GetLiteralAssignments(Expression  exp)
        {
            Visit(exp);

            foreach(var b in initList)
            {
                var r = Expression
                    .Lambda(b.Expression, plist)
                    .Compile()
                    .DynamicInvoke(new object[] { null });

                yield return (b.Member as PropertyInfo, r);

            }
        }

        private List<ParameterExpression> plist = new List<ParameterExpression>();
        private LinkedList<MemberAssignment> initList = new LinkedList<MemberAssignment>();

        private ConstantExpression currentConstant = null;
        readonly EntityProjectionExpression ke;

        protected override Expression VisitMemberInit(MemberInitExpression node)
        {
            foreach (var b in node.Bindings.OfType<MemberAssignment>())
            {
                currentConstant = null;
                this.Visit(b.Expression);
                if (currentConstant != null)
                {
                    initList.AddLast(b);
                }
            }
            return base.VisitMemberInit(node);
        }

        protected override Expression VisitConstant(ConstantExpression node)
        {
            currentConstant = node;
            return base.VisitConstant(node);
        }

        protected override Expression VisitParameter(ParameterExpression node)
        {
            currentConstant = null;
            if (!plist.Any(x => x.Name == node.Name))
                plist.Add(node);
            return base.VisitParameter(node);
        }
    }
}
