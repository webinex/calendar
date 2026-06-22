using System.Diagnostics.CodeAnalysis;
using Webinex.Asky;

namespace Webinex.Calendar.EntityFramework;

internal static class FilterRuleUtil
{
    /// <summary>
    ///     Combines two filter rules with AND operator, but if one of them is null, returns the other or null when both null
    /// </summary>
    /// <param name="left">Left filter rule</param>
    /// <param name="right">Right filter rule</param>
    /// <returns>When both not null - returns AND of them. When one not null - returns not null filter rule. WHen both null - returns null.</returns>
    [return: NotNullIfNotNull(nameof(left))]
    [return: NotNullIfNotNull(nameof(right))]
    public static FilterRule? AndSafe(FilterRule? left, FilterRule? right)
    {
        if (left is null)
            return right;

        if (right is null)
            return left;

        return FilterRule.And(left, right);
    }
}