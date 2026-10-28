using System.Diagnostics.CodeAnalysis;

namespace Webinex.Calendar.Calculators;

public class OccurrenceCalculator<TData>
    where TData : class, ICloneable
{
    private readonly Period<DateTimeOffset> _period;
    private readonly IReadOnlyCollection<Event<TData>> _events;
    private readonly IReadOnlyCollection<Event<TData>> _recurrentEvents;
    private readonly IReadOnlyCollection<OccurrenceAdjustment<TData>> _adjustments;

    public OccurrenceCalculator(Period<DateTimeOffset> period, IReadOnlyCollection<IEventEntityBase> entries)
    {
        _period = period.ToUtc();
        _events = entries.OfType<Event<TData>>().Where(x => x.Recurrence == null).ToArray();
        _recurrentEvents = entries.OfType<Event<TData>>().Where(x => x.Recurrence != null).ToArray();
        _adjustments = entries.OfType<OccurrenceAdjustment<TData>>().ToArray();
    }

    /// <summary>
    ///     Returns occurrences which actual period (after move) intersects period,
    ///     including occurrences moved into period and excluding moved out of it
    /// </summary>
    public IReadOnlyCollection<Occurrence<TData>> Calculate()
    {
        return _events.Select(CalculateOneTime)
            .Concat(_recurrentEvents.SelectMany(CalculateRecurrent))
            .Where(x => x.Period.Intersects(_period))
            .OrderBy(x => x.Period.Start)
            .ToArray();
    }

    public static Occurrence<TData> Calculate(
        OccurrenceId id,
        Event<TData> @event,
        OccurrenceAdjustment<TData>? adjustment)
    {
        return TryCalculate(id, @event, adjustment) ??
               throw new InvalidOperationException(
                   $"Unable to calculate occurrence {id} for event {@event.Id}. See exception Data for more details.")
               {
                   Data =
                   {
                       ["id"] = id,
                       ["event"] = @event,
                       ["adjustment"] = adjustment,
                   }
               };
    }

    /// <summary>
    ///     Returns null when recurrent event doesn't produce occurrence with specified id
    ///     (occurrence cancelled or id doesn't match recurrence anymore)
    /// </summary>
    public static Occurrence<TData>? TryCalculate(
        OccurrenceId id,
        Event<TData> @event,
        OccurrenceAdjustment<TData>? adjustment)
    {
        return @event.Recurrence != null ? TryCalculateRecurrent(id, @event, adjustment) : CalculateOneTime(@event);
    }

    public static Occurrence<TData> CalculateOneTime(Event<TData> @event)
    {
        var occurrenceId = OccurrenceId.OneTime(@event.Id).ToString();
        return new Occurrence<TData>(occurrenceId, @event.Group, @event.Period, @event.Data);
    }

    public static Occurrence<TData> CalculateRecurrent(
        OccurrenceId id,
        Event<TData> @event,
        OccurrenceAdjustment<TData>? adjustment)
    {
        return TryCalculateRecurrent(id, @event, adjustment) ??
               throw new InvalidOperationException(
                   $"Unable to calculate occurrence {id} for event {@event.Id}. See exception Data for more details.")
               {
                   Data =
                   {
                       ["id"] = id,
                       ["event"] = @event,
                       ["adjustment"] = adjustment,
                   }
               };
    }

    private static Occurrence<TData>? TryCalculateRecurrent(
        OccurrenceId id,
        Event<TData> @event,
        OccurrenceAdjustment<TData>? adjustment)
    {
        if (id.Start is null)
            return null;

        // Moved occurrence is searched in its new position, because Calculate returns only occurrences in period
        var start = adjustment?.MoveTo?.Start ?? id.Start.Value;
        IEventEntityBase[] entities = adjustment == null ? [@event] : [@event, adjustment];
        return new OccurrenceCalculator<TData>(Period.New(start, start.AddMinutes(1)), entities)
            .Calculate()
            .FirstOrDefault(x => x.Id == id.ToString());
    }

    public static bool TryCalculateOccurrenceAdjustment(
        OccurrenceAdjustment<TData> adjustment,
        [NotNullWhen(true)] out Occurrence<TData>? occurrence)
    {
        occurrence = null;
        if (adjustment.Data == null)
            return false;

        occurrence = new Occurrence<TData>(
            adjustment.Id,
            adjustment.Group,
            adjustment.MoveTo ?? adjustment.Period,
            adjustment.Data);

        return true;
    }

    private IEnumerable<Occurrence<TData>> CalculateRecurrent(Event<TData> @event)
    {
        var occurrences = CalculateRecurrentEventOccurrences(@event).ToArray();
        return occurrences.Concat(CalculateMovedIntoPeriod(@event, occurrences));
    }

    private IEnumerable<Occurrence<TData>> CalculateRecurrentEventOccurrences(Event<TData> @event)
    {
        foreach (var period in RecurrenceCalculator.Occurrences(@event, _period.ToOpenPeriod()))
        {
            var id = OccurrenceId.Recurrent(@event.Id, period.Start);
            var adjustment = _adjustments.FirstOrDefault(x => x.Id == id.ToString());

            if (adjustment?.Cancelled == true)
                continue;

            yield return new Occurrence<TData>(
                id.ToString(),
                @event.Group,
                adjustment?.MoveTo ?? period,
                adjustment?.Data ?? @event.Data);
        }
    }

    /**
     * We can have recurrent events which doesn't produce occurrences in period, but they <see cref="OccurrenceAdjustment{TData}"/>
     * can be moved into period. In this case <see cref="RecurrenceCalculator"/> will not produce period because
     * it works directly with event and doesn't know that some of the occurrences moved into period.
     * In this method we return such missed occurrences
     */
    private IEnumerable<Occurrence<TData>> CalculateMovedIntoPeriod(
        Event<TData> @event,
        Occurrence<TData>[] occurrences)
    {
        // Adjustments without MoveTo stay on original period, so they can't be moved into period
        foreach (var adjustment in _adjustments.Where(x =>
                     x.RecurrentEventId == @event.Id && !x.Cancelled && x.MoveTo != null))
        {
            if (occurrences.Any(x => x.Id == adjustment.Id))
                continue;

            yield return new Occurrence<TData>(
                adjustment.Id,
                @event.Group,
                adjustment.MoveTo!,
                adjustment.Data ?? @event.Data);
        }
    }
}