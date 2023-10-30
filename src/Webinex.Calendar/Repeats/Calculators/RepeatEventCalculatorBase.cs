using Webinex.Calendar.Events;
using Period = Webinex.Calendar.Common.Period;

namespace Webinex.Calendar.Repeats.Calculators;

internal abstract class RepeatEventCalculatorBase
{
    public abstract IEnumerable<Period> Calculate(RecurrentEvent @event, DateTimeOffset start, DateTimeOffset? end);
}