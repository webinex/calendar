namespace Webinex.Calendar;

public static class PeriodExtensions
{
    public static Period<DateTimeOffset> ToUtc(this Period<DateTimeOffset> value)
    {
        return new Period<DateTimeOffset>(value.Start.ToUniversalTime(), value.End.ToUniversalTime());
    }
    
    public static OpenPeriod<DateTimeOffset> ToOpenPeriod(this Period<DateTimeOffset> value)
    {
        return new OpenPeriod<DateTimeOffset>(value.Start, value.End);
    }

    public static TimeSpan Duration(this Period<DateTimeOffset> period)
    {
        return period.End - period.Start;
    }

    public static Period<DateTimeOffset> Move(this Period<DateTimeOffset> value, TimeSpan timeSpan)
    {
        return Period.New(value.Start.Add(timeSpan), value.End.Add(timeSpan));
    }

    public static bool Contains<T>(this Period<T> period, T value)
        where T : IComparable<T>
    {
        return period.Start.CompareTo(value) <= 0 && period.End.CompareTo(value) > 0;
    }
}