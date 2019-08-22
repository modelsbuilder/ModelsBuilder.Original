using System;
using System.Linq.Expressions;
using System.Reflection;
using NUnit.Framework;

namespace ZpqrtBnk.ModelsBuilder.Tests
{
    [TestFixture]
    public class ExpressionTests
    {
        [Test]
        public void Test()
        {
            var o = new ModelClass();
            var mi = SelectProperty(o, x => x.ValueInt);
        }

        private MemberInfo SelectProperty<TModel, TValue>(TModel model, Expression<Func<TModel, TValue>> property)
        {
            if (property.NodeType != ExpressionType.Lambda)
                throw new Exception("not a lambda: " + property.NodeType);

            var lambda = (LambdaExpression) property;
            var lambdaBody = lambda.Body;

            if (lambdaBody.NodeType != ExpressionType.MemberAccess)
                throw new Exception("not a member access: " + lambdaBody.NodeType);

            var member = (MemberExpression) lambdaBody;
            if (member.Expression.NodeType != ExpressionType.Parameter)
                throw new Exception("not a parameter: " + member.Expression.NodeType);

            return member.Member;
        }

        public static MemberInfo FindProperty(LambdaExpression lambda)
        {
            void Throw()
            {
                throw new ArgumentException($"Expression '{lambda}' must resolve to top-level member and not any child object's properties. Use a custom resolver on the child type or the AfterMap option instead.", nameof(lambda));
            }

            Expression expr = lambda;
            var loop = true;
            string alias = null;
            while (loop)
            {
                switch (expr.NodeType)
                {
                    case ExpressionType.Convert:
                        expr = ((UnaryExpression) expr).Operand;
                        break;
                    case ExpressionType.Lambda:
                        expr = ((LambdaExpression) expr).Body;
                        break;
                    //case ExpressionType.Call:
                    //    var callExpr = (MethodCallExpression) expr;
                    //    var method = callExpr.Method;
                    //    if (method.DeclaringType != typeof(NPocoSqlExtensions.Statics) || method.Name != "Alias" || !(callExpr.Arguments[1] is ConstantExpression aliasExpr))
                    //        Throw();
                    //    expr = callExpr.Arguments[0];
                    //    alias = aliasExpr.Value.ToString();
                    //    break;
                    case ExpressionType.MemberAccess:
                        var memberExpr = (MemberExpression) expr;
                        if (memberExpr.Expression.NodeType != ExpressionType.Parameter && memberExpr.Expression.NodeType != ExpressionType.Convert)
                            Throw();
                        return memberExpr.Member;
                    default:
                        loop = false;
                        break;
                }
            }

            throw new Exception("Configuration for members is only supported for top-level individual members on a type.");
        }
        private class ModelClass
        {
            public int ValueInt { get; set; }
            public string ValueString { get; set; }
        }
    }
}
