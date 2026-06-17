using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;
using NodaTime;
using Webinex.Calendar.Calculators;
using Webinex.Coded;

namespace Webinex.Calendar.Extensions;

public static class EventExtensions
{
    private record EffectiveCacheEntry(
        Period<DateTimeOffset> Period,
        string TimeZone,
        Recurrence? Recurrence,
        OpenPeriod<DateTimeOffset> Result);

    private record FirstOccurrenceCacheEntry(
        Period<DateTimeOffset> Period,
        string TimeZone,
        Recurrence? Recurrence,
        Period<DateTimeOffset>? Result);

    private static readonly ConditionalWeakTable<IEventEntityBase, EffectiveCacheEntry> EffectiveCache = new();
    private static readonly ConditionalWeakTable<IEventEntityBase, FirstOccurrenceCacheEntry> FirstOccurrenceCache = new();

    /// <summary>
    ///     Determines whether the event effective period intersects with the specified period.
    /// </summary>
    [Pure]
    public static bool Intersects(this IEvent @event, Period<DateTimeOffset> period)
    {
        return @event.Effective().Intersects(period);
    }

    /// <summary>
    ///     Returns the full period where the event can produce occurrences.
    /// </summary>
    [Pure]
    public static OpenPeriod<DateTimeOffset> Effective(this IEvent @event)
    {
        if (TryGetEffectiveFromCache(@event, out var cached))
            return cached;

        var start = StartOfFirstOccurrence(@event);
        var result = new OpenPeriod<DateTimeOffset>(start, EndOfLastOccurrence(@event));
        EffectiveCache.AddOrUpdate(@event,
            new EffectiveCacheEntry(@event.Period, @event.TimeZone, @event.Recurrence, result));
        return result;
    }

    private static bool TryGetEffectiveFromCache(
        IEvent @event,
        [NotNullWhen(true)] out OpenPeriod<DateTimeOffset>? result)
    {
        if (EffectiveCache.TryGetValue(@event, out var cached) &&
            cached.Period == @event.Period &&
            cached.TimeZone == @event.TimeZone &&
            cached.Recurrence == @event.Recurrence)
        {
            result = cached.Result;
            return true;
        }

        result = null;
        return false;
    }

    public static void ValidateAtLeastOneOccurrenceOrThrow(this IEvent @event)
    {
        _ = FirstOccurrence(@event);
    }

    /// <summary>
    ///     Returns the actual start of the first occurrence.
    /// </summary>
    [Pure]
    private static DateTimeOffset StartOfFirstOccurrence(this IEvent @event)
    {
        if (@event.Recurrence == null)
            return @event.Period.Start;

        return FirstOccurrence(@event).Start;
    }

    [Pure]
    public static Period<DateTimeOffset> FirstOccurrence(this IEvent @event)
    {
        if (@event.Recurrence == null)
            return @event.Period;

        if (TryGetFirstOccurrenceFromCache(@event, out var cached))
            return cached ?? throw CodedFailure.This.NoOccurrence(@event.Id).Throw();

        var period = new OpenPeriod<DateTimeOffset>(@event.Period.Start, null);
        var firstOccurrence = RecurrenceCalculator.Occurrences(@event, period).FirstOrDefault();
        FirstOccurrenceCache.AddOrUpdate(@event,
            new FirstOccurrenceCacheEntry(@event.Period, @event.TimeZone, @event.Recurrence, firstOccurrence));

        return firstOccurrence ?? throw CodedFailure.This.NoOccurrence(@event.Id).Throw();
    }

    private static bool TryGetFirstOccurrenceFromCache(IEvent @event, out Period<DateTimeOffset>? result)
    {
        if (FirstOccurrenceCache.TryGetValue(@event, out var cached) &&
            cached.Period == @event.Period &&
            cached.TimeZone == @event.TimeZone &&
            cached.Recurrence == @event.Recurrence)
        {
            result = cached.Result;
            return true;
        }

        result = null;
        return false;
    }

    /// <summary>
    ///     Returns the actual end of the last occurrence for finite recurrent events.
    /// </summary>
    [Pure]
    private static DateTimeOffset? EndOfLastOccurrence(this IEvent @event)
    {
        if (@event.Recurrence == null)
            return @event.Period.End;

        if (!@event.Recurrence.MGRecurrence!.Period.End.HasValue)
            return null;

        // Search past the last possible occurrence start so overnight occurrences on the final recurrence date are included.
        var period = new OpenPeriod<DateTimeOffset>(
            @event.Period.Start,
            LastPossibleOccurrenceStartExclusive(@event).Add(@event.Period.Duration()));

        return RecurrenceCalculator.Occurrences(@event, period).LastOrDefault()?.End;
    }

    /// <summary>
    ///     Returns the exclusive upper bound for possible occurrence starts.
    /// </summary>
    private static DateTimeOffset LastPossibleOccurrenceStartExclusive(IEvent @event)
    {
        var tz = DateTimeZoneProviders.Tzdb[@event.TimeZone];
        // MG recurrence end date is inclusive, so the first instant after that date is an exclusive start boundary.
        return @event.Recurrence!.MGRecurrence!.Period.End!.Value.InZone(@event.TimeZone).PlusDays(1)
            .InZoneLeniently(tz).ToDateTimeOffset().ToUniversalTime();
    }

    public static DateOnly? StartOfRecurrencePeriod(this IEvent @event)
    {
        return @event.Recurrence?.MGRecurrence?.Period.Start;
    }
}
