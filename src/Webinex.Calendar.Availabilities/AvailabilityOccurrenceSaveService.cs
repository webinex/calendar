using System.Diagnostics.CodeAnalysis;
using Webinex.Calendar.Calculators;
using Webinex.Calendar.Common;
using Webinex.Calendar.Extensions;

namespace Webinex.Calendar.Availabilities;

/**
    * When you update an availability for a week following rules applied:
    * We assume week contains only <see cref="Event{TData}"/> and no <see cref="OccurrenceAdjustment{TData}"/>
    * If event starts on this week and ends on this week - we delete it
    * If event starts on this week and doesn't end on this week - we move it on a week later
    * If event starts in a past and ends on this week - we end it on a week before
    * If event starts in a past and doesn't end on this week - we end it on a week before and create a new instance for a continuation a week later
 */
internal class AvailabilityOccurrenceSaveService<TData>
    where TData : class, IAvailabilityData
{
    private readonly IEventRepository<TData> _eventRepository;

    public AvailabilityOccurrenceSaveService(IEventRepository<TData> eventRepository)
    {
        _eventRepository = eventRepository;
    }

    public async Task SaveAsync(SaveAvailabilityArgs<TData> args)
    {
        var period = Period.New(args.StartOfWeek, args.StartOfWeek.AddDays(7)).InZone(args.TimeZone);
        var events = await _eventRepository.GetAvailabilityEventsAsync(args.TenantId, args.HostId);

        var operations = events
            .SelectMany(x => MapEventOperations(x, period))
            .Concat(MapAddWeekEventsOperations(args.TimeZone, args.Available)).ToArray();

        await _eventRepository.PatchAsync(operations);
    }

    private IEnumerable<Operation> MapAddWeekEventsOperations(
        string timeZone,
        IEnumerable<SaveAvailabilityArgs<TData>.Item> available)
    {
        foreach (var item in available)
        {
            var occurrenceDate = item.Period.Start.ToDateOnly(timeZone);
            var @event = Event.Factory.MGDaily(item.Period, timeZone, item.Data, until: occurrenceDate);
            yield return Operation.Add(@event);
        }
    }

    private IEnumerable<Operation> MapEventOperations(Event<TData> x, Period<DateTimeOffset> period)
    {
        if (!x.Effective().Intersects(period))
            return [];

        if (TryMapEventOnlyOnThisWeek(x, period, out var result1))
            return result1;

        if (TryMapEventStartsOnThisWeek(x, period, out var result2))
            return result2;

        if (TryMapEventStartsInPastAndEndsOnThisWeek(x, period, out var result3))
            return result3;

        if (TryMapEventStartsInPastAndContinuesNextWeek(x, period, out var result4))
            return result4;

        throw new InvalidOperationException(
            $"Unable to map operations for event {x.Id} when save a week occurrence for a period of {period}");
    }

    private bool TryMapEventOnlyOnThisWeek(
        Event<TData> x,
        Period<DateTimeOffset> period,
        [NotNullWhen(true)] out IEnumerable<Operation>? operations)
    {
        operations = null;
        var isEventStartsAndEndsOnThisWeek = period.Contains(x.Period.Start) && x.Effective().End.HasValue &&
                                             period.Contains(x.Effective().End!.Value);

        if (isEventStartsAndEndsOnThisWeek)
            operations = [Operation.Remove(x)];

        return isEventStartsAndEndsOnThisWeek;
    }

    private bool TryMapEventStartsOnThisWeek(
        Event<TData> x,
        Period<DateTimeOffset> period,
        [NotNullWhen(true)] out IEnumerable<Operation>? operations)
    {
        operations = null;
        var isEventStartsOnThisWeek = period.Contains(x.Period.Start);

        if (!isEventStartsOnThisWeek)
            return isEventStartsOnThisWeek;

        operations = MapEventStartsOnThisWeek(x, period);
        return true;
    }

    private IEnumerable<Operation> MapEventStartsOnThisWeek(
        Event<TData> x,
        Period<DateTimeOffset> period)
    {
        var nextWeekContinuationEvent = NewNextWeekContinuationEvent(x, period);
        yield return Operation.Remove(x);
        if (nextWeekContinuationEvent != null) yield return Operation.Add(nextWeekContinuationEvent);
    }

    private bool TryMapEventStartsInPastAndEndsOnThisWeek(
        Event<TData> x,
        Period<DateTimeOffset> period,
        [NotNullWhen(true)] out IEnumerable<Operation>? operations)
    {
        operations = null;
        var isEventStartsInPastAndEndsOnThisWeek =
            x.Period.Start < period.Start && x.Effective().End.HasValue && x.Effective().End!.Value <= period.End;

        if (!isEventStartsInPastAndEndsOnThisWeek)
            return false;

        x.SetEndDate(period.End.ToDateOnly(x.TimeZone).AddDays(-8)); // -8 because Until is inclusive
        operations = [Operation.Update(x)];
        return true;
    }

    private bool TryMapEventStartsInPastAndContinuesNextWeek(
        Event<TData> x,
        Period<DateTimeOffset> period,
        [NotNullWhen(true)] out IEnumerable<Operation>? operations)
    {
        operations = null;
        var isEventStartsInPastAndContinuesNextWeek =
            x.Period.Start < period.Start && (!x.Effective().End.HasValue || x.Effective().End!.Value > period.End);

        if (!isEventStartsInPastAndContinuesNextWeek)
            return false;

        operations = MapEventStartsInPastAndContinuesNextWeek(x, period);
        return true;
    }

    private IEnumerable<Operation> MapEventStartsInPastAndContinuesNextWeek(
        Event<TData> x,
        Period<DateTimeOffset> period)
    {
        var nextWeekContinuationEvent = NewNextWeekContinuationEvent(x, period);
        x.SetEndDate(period.End.ToDateOnly(x.TimeZone).AddDays(-8)); // -8 because Until is inclusive
        yield return Operation.Update(x);
        if (nextWeekContinuationEvent != null) yield return Operation.Add(nextWeekContinuationEvent);
    }

    private Event<TData>? NewNextWeekContinuationEvent(
        Event<TData> x,
        Period<DateTimeOffset> period)
    {
        var nextOccurrence = RecurrenceCalculator.Occurrences(x, new OpenPeriod<DateTimeOffset>(period.End, null))
            .FirstOrDefault();

        if (nextOccurrence == null)
            return null;

        var startDate = nextOccurrence.Start.ToDateOnly(x.TimeZone);

        return Event.New(
            Period.New(nextOccurrence.Start, nextOccurrence.End),
            x.TimeZone,
            x.Data,
            x.Recurrence!.WithStart(startDate),
            group: x.Group);
    }
}