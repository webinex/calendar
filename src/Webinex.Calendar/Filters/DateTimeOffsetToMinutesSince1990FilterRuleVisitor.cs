using Webinex.Asky;
using Webinex.Calendar.Common;

namespace Webinex.Calendar.Filters;

public class DateTimeOffsetToMinutesSince1990FilterRuleVisitor : FilterRuleVisitor
{
    private readonly string[] _fieldNames;

    public DateTimeOffsetToMinutesSince1990FilterRuleVisitor(string[] fieldNames)
    {
        _fieldNames = fieldNames;
    }

    public override FilterRule? Visit(ValueFilterRule valueFilterRule)
    {
        var match = _fieldNames.Contains(valueFilterRule.FieldId) && valueFilterRule.Value != null!;

        if (!match)
        {
            return valueFilterRule;
        }

        if (valueFilterRule.Value is not DateTimeOffset dateTimeOffset)
            throw new InvalidOperationException($"Value might be {nameof(DateTimeOffset)}");

        return new ValueFilterRule(
            valueFilterRule.FieldId,
            valueFilterRule.Operator,
            dateTimeOffset.TotalMinutesSince1990());
    }
}