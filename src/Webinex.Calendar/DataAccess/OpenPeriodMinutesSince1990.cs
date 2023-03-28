using Webinex.Calendar.Common;

namespace Webinex.Calendar.DataAccess;

public class OpenPeriodMinutesSince1990 : Equatable
{
    protected OpenPeriodMinutesSince1990()
    {
    }

    public OpenPeriodMinutesSince1990(DateTimeOffset start, DateTimeOffset? end)
        : this(start.ToUtc().TotalMinutesSince1990(), end?.ToUtc().TotalMinutesSince1990())
    {
    }

    public OpenPeriodMinutesSince1990(Period period)
        : this(period.Start.ToUtc().TotalMinutesSince1990(), period.End.ToUtc().TotalMinutesSince1990())
    {
    }

    public OpenPeriodMinutesSince1990(OpenPeriod period)
        : this(period.Start.ToUtc().TotalMinutesSince1990(), period.End?.ToUtc().TotalMinutesSince1990())
    {
    }

    public OpenPeriodMinutesSince1990(long start, long? end)
    {
        if (end.HasValue && end <= start)
            throw new InvalidOperationException("End might not be less than or equal to start");

        Start = start;
        End = end;
    }

    public long Start { get; protected set; }
    public long? End { get; protected set; }

    public OpenPeriod ToOpenPeriod()
    {
        return new OpenPeriod(Constants.J1_1990.AddMinutes(Start),
            End.HasValue ? Constants.J1_1990.AddMinutes(End.Value) : null);
    }

    public Period ToPeriod()
    {
        return ToOpenPeriod().ToPeriod();
    }

    public static bool operator ==(OpenPeriodMinutesSince1990? left, OpenPeriodMinutesSince1990? right)
    {
        return EqualOperator(left, right);
    }

    public static bool operator !=(OpenPeriodMinutesSince1990? left, OpenPeriodMinutesSince1990? right)
    {
        return NotEqualOperator(left, right);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Start;
        yield return End;
    }
}