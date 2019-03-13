using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace EFCoreBulk
{
    internal class LiteralExpressionVisitor : ExpressionVisitor
    {

        readonly Expression exp;
        IList<MemberAssignment> list;
        public LiteralExpressionVisitor(Expression exp)
        {
            this.exp = exp;
        }

        public IList<MemberAssignment> GetLiteralAssignments() {
            list = new List<MemberAssignment>();
            Visit(exp);
            return list;
        }

        protected override Expression VisitMemberInit(MemberInitExpression node)
        {
            foreach (var b in node.Bindings.OfType<MemberAssignment>()
                .Where(x => x.Expression is ConstantExpression)) {
                list.Add(b);
            }
            return base.VisitMemberInit(node);
        }
    }
}
