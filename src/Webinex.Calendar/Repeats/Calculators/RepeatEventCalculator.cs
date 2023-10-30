using Webinex.Calendar.Events;
using Period = Webinex.Calendar.Common.Period;

namespace Webinex.Calendar.Repeats.Calculators;

internal static class RepeatEventCalculator
{
    public static IEnumerable<Period> Matches(
        RecurrentEvent @event,
        DateTimeOffset start,
        DateTimeOffset? end)
    {
        if (@event.Repeat.DayOfMonth != null)
            return new DayOfMonthRepeatEventCalculator().Calculate(@event, start, end);
        
        if (@event.Repeat.Weekday != null)
            return new WeekdayRepeatEventCalculator().Calculate(@event, start, end);
        
        if (@event.Repeat.Interval != null)
            return new IntervalRepeatEventCalculator().Calculate(@event, start, end);

        throw new InvalidOperationException();
    }
}