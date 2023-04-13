using Webinex.Asky;
using Webinex.Calendar.Common;

namespace Webinex.Calendar.Filters;

internal class RecurrentEventFilterRuleReplaceDateTimeOffsetVisitor : FilterRuleVisitor
{
    public override FilterRule? Visit(ValueFilterRule valueFilterRule)
    {
        var match = valueFilterRule.FieldId is "effective.start" or "effective.end"
                    && valueFilterRule.Value != null!;
        
        if (!match)
            return valueFilterRule;

        if (valueFilterRule.Value is not DateTimeOffset dateTimeOffset)
            throw new InvalidOperationException($"Value might be {nameof(DateTimeOffset)}");

        return new ValueFilterRule(valueFilterRule.FieldId, valueFilterRule.Operator,
            dateTimeOffset.TotalMinutesSince1990());
    }
}