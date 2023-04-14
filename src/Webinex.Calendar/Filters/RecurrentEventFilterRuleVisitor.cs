using Webinex.Asky;
using Webinex.Calendar.Common;

namespace Webinex.Calendar.Filters;

internal class RecurrentEventFilterRuleVisitor : FilterRuleVisitor
{
    public override FilterRule? Visit(ValueFilterRule valueFilterRule)
    {
        if (TryVisitEffective(valueFilterRule, out var effectiveRule))
            return effectiveRule;

        if (TryVisitRepeatWeekdays(valueFilterRule, out var repeatWeekdaysRule))
            return repeatWeekdaysRule;
        
        return valueFilterRule;
    }

    private bool TryVisitRepeatWeekdays(ValueFilterRule valueFilterRule, out FilterRule? result)
    {
        var operatorMatch = valueFilterRule.Operator is FilterOperator.CONTAINS or FilterOperator.NOT_CONTAINS;
        var fieldIdMatch = valueFilterRule.FieldId == "repeat.weekday.weekdays";

        if (!operatorMatch || !fieldIdMatch)
        {
            result = null;
            return false;
        }

        result = CreateWeekdayFilterRule(valueFilterRule, valueFilterRule.Operator == FilterOperator.CONTAINS);
        return true;
    }

    private FilterRule CreateWeekdayFilterRule(ValueFilterRule valueFilterRule, bool expectedValue)
    {
        var value = valueFilterRule.Value as IEnumerable<Weekday>;
        value = value?.Distinct().ToArray() ?? throw new InvalidOperationException(
            $"The value of filter rule for field id `repeat.weekday.weekdays` might be a collection of Weekday");

        if (!value.Any())
            throw new InvalidOperationException(
                "The value of filter rule for field id `repeat.weekday.weekdays` might contain at least one weekday");

        var rules = value.Select(weekday => FilterRule.Eq($"repeat.weekday.{weekday.Value.ToLower()}", expectedValue)).ToArray();
        return rules.Count() > 1 ? FilterRule.And(rules) : rules.ElementAt(0);
    }

    private bool TryVisitEffective(ValueFilterRule valueFilterRule, out FilterRule? result)
    {
        var match = valueFilterRule.FieldId is "effective.start" or "effective.end"
                    && valueFilterRule.Value != null!;

        if (!match)
        {
            result = null;
            return false;
        }

        if (valueFilterRule.Value is not DateTimeOffset dateTimeOffset)
            throw new InvalidOperationException($"Value might be {nameof(DateTimeOffset)}");

        result = new ValueFilterRule(valueFilterRule.FieldId, valueFilterRule.Operator,
            dateTimeOffset.TotalMinutesSince1990());

        return true;
    }
}