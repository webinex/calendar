using Webinex.Calendar.MicrosoftGraph;

namespace Webinex.Calendar.Extensions;

public static class EventFactoryExtensions
{
    public static Event<TData> MGDaily<TData>(
        this EventFactory _,
        Period<DateTimeOffset> period,
        string timeZone,
        TData data,
        DateOnly? until = null,
        int interval = 1)
        where TData : class, ICloneable
    {
        var periodTz = period.InZone(timeZone);
        
        var mgRecurrence = new MGRecurrence(
            new MGRecurrencePeriod(periodTz.Start.Date.ToDateOnly(), until),
            MGRecurrencePattern.Daily(interval));

        var recurrence = new Recurrence(mgRecurrence);
        return Event<TData>.New(period, timeZone, data, recurrence);
    }

    public static Event<TData> MGWeekly<TData>(
        this EventFactory _,
        Period<DateTimeOffset> period,
        string timeZone,
        TData data,
        IEnumerable<DayOfWeek> dayOfWeeks,
        int interval = 1,
        DateOnly? until = null,
        DayOfWeek? firstDayOfWeek = null)
        where TData : class, ICloneable
    {
        var periodTz = period.InZone(timeZone);
        
        var mgRecurrence = new MGRecurrence(
            new MGRecurrencePeriod(periodTz.Start.Date.ToDateOnly(), until),
            MGRecurrencePattern.Weekly(dayOfWeeks, interval, firstDayOfWeek));

        var recurrence = new Recurrence(mgRecurrence);
        return Event<TData>.New(period, timeZone, data, recurrence);
    }

    public static Event<TData> MGAbsoluteMonthly<TData>(
        this EventFactory _,
        Period<DateTimeOffset> period,
        string timeZone,
        TData data,
        int dayOfMonth,
        DateOnly? until = null,
        int interval = 1)
        where TData : class, ICloneable
    {
        var periodTz = period.InZone(timeZone);
        
        var mgRecurrence = new MGRecurrence(
            new MGRecurrencePeriod(periodTz.Start.Date.ToDateOnly(), until),
            MGRecurrencePattern.AbsoluteMonthly(dayOfMonth, interval));

        var recurrence = new Recurrence(mgRecurrence);
        return Event<TData>.New(period, timeZone, data, recurrence);
    }
}