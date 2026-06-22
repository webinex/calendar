using System.Diagnostics.CodeAnalysis;
using Webinex.Calendar.Common;

namespace Webinex.Calendar.Calculators;

public class OccurrenceCalculator<TData>
    where TData : class, ICloneable
{
    private readonly OpenPeriod<DateTimeOffset> _period;
    private readonly IReadOnlyCollection<Event<TData>> _events;
    private readonly IReadOnlyCollection<Event<TData>> _recurrentEvents;
    private readonly IReadOnlyCollection<OccurrenceAdjustment<TData>> _adjustments;

    public OccurrenceCalculator(OpenPeriod<DateTimeOffset> period, IReadOnlyCollection<IEventEntityBase> entries)
    {
        _period = period.ToUtc();
        _events = entries.OfType<Event<TData>>().Where(x => x.Recurrence == null).ToArray();
        _recurrentEvents = entries.OfType<Event<TData>>().Where(x => x.Recurrence != null).ToArray();
        _adjustments = entries.OfType<OccurrenceAdjustment<TData>>().ToArray();
    }

    public OccurrenceCalculator(Period<DateTimeOffset> period, IReadOnlyCollection<IEventEntityBase> entries)
        : this(period.ToOpenPeriod(), entries)
    {
    }

    public IReadOnlyCollection<Occurrence<TData>> Calculate()
    {
        return CalculateEnumerable()
            .OrderBy(x => x.Period.Start)
            .ToArray();
    }

    public IEnumerable<Occurrence<TData>> CalculateEnumerable()
    {
        return EnumerableUtil.MergeOrdered(
            _events.Select(x => new[] { CalculateOneTime(x) })
                .Concat(_recurrentEvents.Select(CalculateRecurrent)),
            x => x.Period.Start);
    }

    public static Occurrence<TData> Calculate(
        OccurrenceId id,
        Event<TData> @event,
        OccurrenceAdjustment<TData>? adjustment)
    {
        return @event.Recurrence != null ? CalculateRecurrent(id, @event, adjustment) : CalculateOneTime(@event);
    }

    public static Occurrence<TData> CalculateOneTime(Event<TData> @event)
    {
        var occurrenceId = new OccurrenceId(@event.Id, @event.Period.Start).ToString();
        return new Occurrence<TData>(occurrenceId, @event.TimeZone, @event.Group, @event.Period, @event.Data);
    }

    public static Occurrence<TData> CalculateRecurrent(
        OccurrenceId id,
        Event<TData> @event,
        OccurrenceAdjustment<TData>? adjustment)
    {
        IEventEntityBase[] entities = adjustment == null ? [@event] : [@event, adjustment];
        return new OccurrenceCalculator<TData>(
                new OpenPeriod<DateTimeOffset>(id.Start, id.Start.AddMinutes(1)),
                entities).Calculate()
            .First();
    }

    public static Occurrence<TData> CalculateOccurrenceAdjustment(
        Event<TData> @event,
        OccurrenceAdjustment<TData> adjustment)
    {
        return new Occurrence<TData>(
            adjustment.Id,
            @event.TimeZone,
            adjustment.Group,
            adjustment.MoveTo ?? adjustment.Period,
            adjustment.Data ?? @event.Data);
    }

    private IEnumerable<Occurrence<TData>> CalculateRecurrent(Event<TData> @event)
    {
        return EnumerableUtil.MergeOrdered(
            [
                CalculateRecurrentEventOccurrences(@event),
                CalculateMovedIntoPeriod(@event),
            ],
            x => x.Period.Start);
    }

    private IEnumerable<Occurrence<TData>> CalculateRecurrentEventOccurrences(Event<TData> @event)
    {
        foreach (var period in RecurrenceCalculator.Occurrences(@event, _period))
        {
            var id = new OccurrenceId(@event.Id, period.Start);
            var adjustment = _adjustments.FirstOrDefault(x => x.Id == id.ToString());

            if (adjustment?.Cancelled == true)
                continue;

            if (adjustment?.MoveTo != null)
                continue;

            yield return new Occurrence<TData>(
                id.ToString(),
                @event.TimeZone,
                @event.Group,
                period,
                adjustment?.Data ?? @event.Data);
        }
    }

    /**
     * We can have recurrent events which doesn't produce occurrences in period, but they <see cref="OccurrenceAdjustment{TData}"/>
     * can be moved into period. In this case <see cref="RecurrenceCalculator"/> will not produce period because
     * it works directly with event and doesn't know that some of the occurrences moved into period.
     * In this method we return such missed occurrences
     */
    private IEnumerable<Occurrence<TData>> CalculateMovedIntoPeriod(Event<TData> @event)
    {
        foreach (var adjustment in _adjustments
                     .Where(x => x.RecurrentEventId == @event.Id && !x.Cancelled && x.MoveTo != null)
                     .Where(x => _period.Intersects(x.MoveTo!))
                     .OrderBy(x => x.MoveTo!.Start))
        {
            yield return new Occurrence<TData>(
                adjustment.Id,
                @event.TimeZone,
                @event.Group,
                adjustment.MoveTo!,
                adjustment.Data ?? @event.Data);
        }
    }
}