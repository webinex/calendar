using System.Linq.Expressions;

namespace Webinex.Calendar.Filters;

internal static class Expressions
{
    public static Expression<Func<T, bool>>? AndOrNull<T>(
        params Expression<Func<T, bool>>[] expressions)
    {
        if (expressions.Length == 0)
            return null;

        if (expressions.Length == 1)
            return expressions[0];

        return And(expressions);
    }

    public static Expression<Func<T, bool>> And<T>(
        Expression<Func<T, bool>> expr1,
        Expression<Func<T, bool>> expr2)
    {
        return GetAggregatedExpression(Expression.AndAlso, expr1, expr2);
    }
    
    public static Expression<Func<T, bool>>? NullableOrOrNull<T>(
        params Expression<Func<T, bool>>?[] expressions)
    {
        expressions = expressions.Where(x => x != null).ToArray();
        
        if (expressions.Length == 0)
            return null;

        if (expressions.Length == 1)
            return expressions[0];

        return Or(expressions!);
    }
    
    public static Expression<Func<T, bool>>? NullableAndOrNull<T>(
        params Expression<Func<T, bool>>?[] expressions)
    {
        expressions = expressions.Where(x => x != null).ToArray();
        
        if (expressions.Length == 0)
            return null;

        if (expressions.Length == 1)
            return expressions[0];

        return And(expressions!);
    }

    public static Expression<Func<T, bool>> Or<T>(
        Expression<Func<T, bool>>[] expressions)
    {
        if (expressions.Length < 2)
        {
            throw new ArgumentException("Might be at least 2 expressions", nameof(expressions));
        }

        return expressions.Skip(1).Aggregate(expressions.ElementAt(0), Or);
    }

    public static Expression<Func<T, bool>> And<T>(
        Expression<Func<T, bool>>[] expressions)
    {
        if (expressions.Length < 2)
        {
            throw new ArgumentException("Might be at least 2 expressions", nameof(expressions));
        }

        return expressions.Skip(1).Aggregate(expressions.ElementAt(0), And);
    }

    public static Expression<Func<T, bool>> Or<T>(
        Expression<Func<T, bool>> expr1,
        Expression<Func<T, bool>> expr2)
    {
        return GetAggregatedExpression(Expression.Or, expr1, expr2);
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

    internal static Expression ReplaceParameter(
        Expression expression,
        ParameterExpression oldParameter,
        ParameterExpression newParameter)
    {
        return new ReplaceExpressionVisitor(oldParameter, newParameter).Visit(expression)!;
    }

    public static Expression<Func<TEntity, bool>> Eq<TEntity>(
        Expression<Func<TEntity, object>> selector,
        object value)
    {
        var parameter = Expression.Parameter(typeof(TEntity));
        var valueType = LambdaExpressions.ReturnType(selector);
        var propertyExpression = LambdaExpressions.PropertyAccessExpression(selector, parameter);
        var valueExpression = Expression.Constant(value, valueType);

        return Expression.Lambda<Func<TEntity, bool>>(
            Expression.Equal(
                propertyExpression,
                valueExpression),
            parameter);
    }

    public static Expression<Func<TEntity, bool>> Child<TEntity, TChild>(
        Expression<Func<TEntity, TChild>> selector,
        Expression<Func<TChild, bool>> predicate)
    {
        var parameter = Expression.Parameter(typeof(TEntity));
        var propertyAccessExpression = LambdaExpressions.PropertyAccessExpression(selector, parameter);
        var newPredicate = new ReplaceExpressionVisitor(predicate.Parameters[0], propertyAccessExpression).Visit(predicate.Body)!;
        return Expression.Lambda<Func<TEntity, bool>>(newPredicate, parameter);
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