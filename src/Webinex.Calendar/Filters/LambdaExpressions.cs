using System.Linq.Expressions;
using System.Reflection;

namespace Webinex.Calendar.Filters;

internal static class LambdaExpressions
{
    private static readonly MethodInfo REPLACE_RETURN_TYPE_TO_TYPED_INTERNAL_METHOD_INFO =
        typeof(LambdaExpressions).GetMethod(nameof(ReplaceReturnTypeToTypedInternal),
            BindingFlags.Static | BindingFlags.NonPublic)!;

    internal static Type ReturnType<TEntity>(Expression<Func<TEntity, object>> selector)
    {
        selector = selector ?? throw new ArgumentNullException(nameof(selector));

        if (selector.Body is MethodCallExpression methodCall)
        {
            return methodCall.Method.ReturnType;
        }
            
        return ((PropertyInfo)PropertyAccessExpression(selector.Body).Member).PropertyType;
    }

    internal static Type ReturnCollectionValueType<TEntity>(Expression<Func<TEntity, object>> selector)
    {
        selector = selector ?? throw new ArgumentNullException(nameof(selector));
        var collectionType = ReturnType(selector);
        
        var valueType = collectionType
            .GetInterfaces()
            .Where(t => t.IsGenericType
                        && t.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            .Select(t => t.GetGenericArguments()[0])
            .FirstOrDefault();

        return valueType ??
               throw new InvalidOperationException($"{collectionType.Name} doesn't inherit generic enumerable");
    }

    internal static object ReplaceReturnTypeToTyped<TEntity>(Expression<Func<TEntity, object>> selector)
    {
        selector = selector ?? throw new ArgumentNullException(nameof(selector));
        var keyType = ReturnType(selector);
        return REPLACE_RETURN_TYPE_TO_TYPED_INTERNAL_METHOD_INFO.MakeGenericMethod(typeof(TEntity), keyType)
            .Invoke(null, new object[] { selector })!;
    }

    private static Expression<Func<TEntity, TKey>> ReplaceReturnTypeToTypedInternal<TEntity, TKey>(
        Expression<Func<TEntity, object>> selector)
    {
        var normalizedSelector = PropertyAccessExpression(selector.Body);
        return Expression.Lambda<Func<TEntity, TKey>>(normalizedSelector, selector.Parameters.ToArray());
    }

    internal static Expression PropertyAccessExpression<TEntity>(
        Expression<Func<TEntity, object>> selector,
        ParameterExpression parameter)
    {
        return Expressions.ReplaceParameter(
            PropertyAccessExpression(selector.Body),
            selector.Parameters[0],
            parameter);
    }

    internal static Expression PropertyAccessExpression<TEntity, TResult>(
        Expression<Func<TEntity, TResult>> selector,
        ParameterExpression parameter)
    {
        return Expressions.ReplaceParameter(
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
}