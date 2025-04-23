using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;
using NodaTime;
using NodaTime.Extensions;
using Webinex.Calendar.Calculators;

namespace Webinex.Calendar.Extensions;

public static class EventExtensions
{
    private static readonly ConditionalWeakTable<IEventEntityBase, OpenPeriod<DateTimeOffset>> Cache = new();

    [Pure]
    public static bool Intersects(this IEvent @event, Period<DateTimeOffset> period)
    {
        return @event.Effective().Intersects(period);
    }

    [Pure]
    public static OpenPeriod<DateTimeOffset> Effective(this IEvent @event)
    {
        if (Cache.TryGetValue(@event, out var cached))
            return cached;

        var start = @event.Period.Start;
        var result = new OpenPeriod<DateTimeOffset>(start, LastOccurrenceEnd(@event));
        Cache.AddOrUpdate(@event, result);
        return result;
    }

    private static DateTimeOffset? LastOccurrenceEnd(this IEvent @event)
    {
        if (@event.Recurrence == null)
            return @event.Period.End;

        if (!@event.Recurrence.MGRecurrence!.Period.End.HasValue)
            return null;

        var tz = DateTimeZoneProviders.Tzdb[@event.TimeZone];
        var eventPeriodTz = @event.Period.InZone(@event.TimeZone);
        var endUtc = @event.Recurrence.MGRecurrence!.Period.End.Value.ToLocalDate().At(eventPeriodTz.End.TimeOfDay)
            .InZoneLeniently(tz).ToDateTimeOffset().ToUniversalTime();
        var period = new OpenPeriod<DateTimeOffset>(@event.Period.Start, endUtc);
        return RecurrenceCalculator.Occurrences(@event, period).LastOrDefault()?.End;
    }
}