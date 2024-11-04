using System.Linq.Expressions;
using System.Reflection;

namespace Webinex.Calendar.Filters;

internal static class Expressions
{
    public static Expression<Func<T, bool>> And<T>(
        Expression<Func<T, bool>> expr1,
        Expression<Func<T, bool>> expr2)
    {
        return GetAggregatedExpression(Expression.AndAlso, expr1, expr2);
    }

    public static Expression<Func<T, bool>> Or<T>(
        IEnumerable<Expression<Func<T, bool>>> expressions)
    {
        expressions = expressions.ToArray();

        return GetAggregatedExpression(Expression.OrElse, expressions.ToArray());
    }

    public static Expression<Func<T, bool>> Or<T>(
        Expression<Func<T, bool>> expr1,
        Expression<Func<T, bool>> expr2)
    {
        return GetAggregatedExpression(Expression.OrElse, expr1, expr2);
    }

    private static Expression<Func<T, bool>> GetAggregatedExpression<T>(
        Func<Expression, Expression, BinaryExpression> aggregate,
        Expression<Func<T, bool>>[] expressions)
    {
        if (expressions.Length == 0)
            throw new ArgumentException("Might be at least 1 expression", nameof(expressions));

        if (expressions.Length == 1)
            return expressions[0];

        var parameter = Expression.Parameter(typeof(T));

        var result = expressions.Skip(1).Aggregate(
            ReplaceParameter(expressions[0].Body, expressions[0].Parameters[0], parameter),
            (current, expression) =>
                aggregate(current, ReplaceParameter(expression.Body, expression.Parameters[0], parameter)));

        return Expression.Lambda<Func<T, bool>>(result, parameter);
    }

    private static Expression<Func<T, bool>> GetAggregatedExpression<T>(
        Func<Expression, Expression, BinaryExpression> aggregate,
        Expression<Func<T, bool>> expr1,
        Expression<Func<T, bool>> expr2)
    {
        var parameter = Expression.Parameter(typeof(T));
        var left = ReplaceParameter(expr1.Body, expr1.Parameters[0], parameter);
        var right = ReplaceParameter(expr2.Body, expr2.Parameters[0], parameter);

        return Expression.Lambda<Func<T, bool>>(aggregate(left, right), parameter);
    }

    private static Expression ReplaceParameter(
        Expression expression,
        ParameterExpression oldParameter,
        ParameterExpression newParameter)
    {
        return new ReplaceExpressionVisitor(oldParameter, newParameter).Visit(expression)!;
    }

    public static Expression<Func<TEntity, bool>> Child<TEntity, TChild>(
        Expression<Func<TEntity, TChild>> selector,
        Expression<Func<TChild, bool>> predicate)
    {
        var parameter = Expression.Parameter(typeof(TEntity));
        var propertyAccessExpression = PropertyAccessExpression(selector, parameter);
        var newPredicate =
            new ReplaceExpressionVisitor(predicate.Parameters[0], propertyAccessExpression).Visit(predicate.Body)!;
        return Expression.Lambda<Func<TEntity, bool>>(newPredicate, parameter);
    }

    private static Expression PropertyAccessExpression<TEntity, TResult>(
        Expression<Func<TEntity, TResult>> selector,
        ParameterExpression parameter)
    {
        return ReplaceParameter(
            PropertyAccessExpression(selector.Body),
            selector.Parameters[0],
            parameter);
    }

    internal static MemberExpression PropertyAccessExpression(Expression expression)
    {
        switch (expression)
        {
            case MemberExpression memberExpression:
                if (!memberExpression.Member.MemberType.HasFlag(MemberTypes.Property))
                    throw new InvalidOperationException($"Member {memberExpression.Member.Name} isn't a property");
                return memberExpression;

            case UnaryExpression unaryExpression:
                if (unaryExpression.NodeType == ExpressionType.Convert &&
                    unaryExpression.Operand.NodeType == ExpressionType.MemberAccess)
                    return PropertyAccessExpression(unaryExpression.Operand);
                throw new InvalidOperationException(
                    $"Unable to resolve property from unary expression {unaryExpression.NodeType}");

            default:
                throw new InvalidOperationException(
                    $"Unable to resolve property access from expression of type {expression.GetType().Name}");
        }
    }

    private class ReplaceExpressionVisitor
        : ExpressionVisitor
    {
        private readonly Expression _oldValue;
        private readonly Expression _newValue;

        public ReplaceExpressionVisitor(Expression oldValue, Expression newValue)
        {
            _oldValue = oldValue;
            _newValue = newValue;
        }

        public override Expression? Visit(Expression? node)
        {
            if (node == _oldValue)
            {
                return _newValue;
            }

            return base.Visit(node);
        }
    }
}