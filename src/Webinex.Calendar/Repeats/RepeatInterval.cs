using Webinex.Calendar.Common;

namespace Webinex.Calendar.Repeats;

public class RepeatInterval : ValueObject
{
    protected RepeatInterval()
    {
    }

    public long StartSince1990Minutes { get; protected set; }
    public long? EndSince1990Minutes { get; protected set; }
    public int DurationMinutes { get; protected set; }
    public int IntervalMinutes { get; protected set; }

    public DateTimeOffset Start() => Constants.J1_1990.AddMinutes(StartSince1990Minutes);

    public static RepeatInterval New(DateTimeOffset start, DateTimeOffset? end, int intervalMinutes, int durationMinutes)
    {
        if (start.Second > 0 || start.Millisecond > 0)
            throw new ArgumentException("Might not contain seconds and milliseconds", nameof(start));
        
        if (end.HasValue && (end.Value.Second > 0 || end.Value.Millisecond > 0))
            throw new ArgumentException("Might not contain seconds and milliseconds", nameof(end));

        var startSince1990Minutes = (int)(start.ToOffset(TimeSpan.Zero) - Constants.J1_1990).TotalMinutes;

        return new RepeatInterval
        {
            StartSince1990Minutes = startSince1990Minutes,
            EndSince1990Minutes = end.HasValue ? (int)(end.Value.ToUtc() - Constants.J1_1990).TotalMinutes : null,
            IntervalMinutes = intervalMinutes,
            DurationMinutes = durationMinutes,
        };
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return StartSince1990Minutes;
        yield return IntervalMinutes;
        yield return DurationMinutes;
    }

    public static bool operator ==(RepeatInterval? left, RepeatInterval? right)
    {
        return EqualOperator(left, right);
    }

    public static bool operator !=(RepeatInterval? left, RepeatInterval? right)
    {
        return NotEqualOperator(left, right);
    }
}