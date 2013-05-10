using System;
using System.Linq.Expressions;

namespace EsentMapper.Mapping
{
	internal static class ExpressionExtensions
	{
		 public static Member ToMember<TMapping, TReturn>(this Expression<Func<TMapping, TReturn>> expression)
		 {
			return GetMember(expression);
		 }

		 private static Member GetMember<TMapping, TReturn>(Expression<Func<TMapping, TReturn>> expression)
		 {
			return GetMember(expression.Body);
		 }

		 private static Member GetMember(Expression expression)
		 {
			 MemberExpression memberExpression = null;
			 switch (expression.NodeType)
			 {
			 	case ExpressionType.Convert:
			 	{
			 		var body = (UnaryExpression)expression;
			 		memberExpression = body.Operand as MemberExpression;
			 	}
			 		break;
			 	case ExpressionType.MemberAccess:
			 		memberExpression = expression as MemberExpression;
			 		break;
			 }

		 	if (memberExpression == null)
		 		throw new ArgumentException("Not a member access", "expression");

		 	return memberExpression.Member.ToMember();
		 }
	}
}