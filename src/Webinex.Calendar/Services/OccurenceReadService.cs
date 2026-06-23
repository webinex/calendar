using Webinex.Asky;
using Webinex.Calendar.Calculators;
using Webinex.Calendar.Extensions;

namespace Webinex.Calendar.Services;

internal interface IOccurrenceReadService<TData>
    where TData : class, ICloneable
{
    Task<IReadOnlyCollection<Occurrence<TData>>> OccurrencesAsync(
        Period<DateTimeOffset> period,
        FilterRule? dataFilterRule = null,
        bool tryCache = false);

    Task<ILookup<string, Occurrence<TData>>> OccurrencesByEventAsync(OccurrencesByEventQueryArgs args);

    Task<IReadOnlyCollection<Occurrence<TData>>> OccurrencesAsync(IEnumerable<string> ids, bool tryCache = false);
}

internal class OccurrenceReadService<TData> : IOccurrenceReadService<TData>
    where TData : class, ICloneable
{
    private readonly IEventRepository<TData> _eventRepository;
    private readonly IAskyFieldMap<Occurrence<TData>> _occurrenceFieldMap;

    public OccurrenceReadService(
        IEventRepository<TData> eventRepository,
        IAskyFieldMap<Occurrence<TData>> occurrenceFieldMap)
    {
        _eventRepository = eventRepository;
        _occurrenceFieldMap = occurrenceFieldMap;
    }

    public async Task<IReadOnlyCollection<Occurrence<TData>>> OccurrencesAsync(
        Period<DateTimeOffset> period,
        FilterRule? dataFilterRule = null,
        bool tryCache = false)
    {
        var matches = await _eventRepository.MatchAsync(period, dataFilterRule);
        var result = new OccurrenceCalculator<TData>(period, matches).Calculate();
        result = result.Where(x => x.Period.Intersects(period)).ToArray();
        return dataFilterRule == null ? result : FilterByDataFilterRule(result, dataFilterRule);
    }

    public async Task<ILookup<string, Occurrence<TData>>> OccurrencesByEventAsync(OccurrencesByEventQueryArgs args)
    {
        var entities = await _eventRepository.ByIdAsync<Event<TData>>(args.Ids);
        var adjustments = args.IsRespectAdjustments ? await FetchAdjustmentsByEventsAsync(entities) : null;
        return Calculate(entities, adjustments, args.Period, args.Count);
    }

    private ILookup<string, Occurrence<TData>> Calculate(
        IReadOnlyCollection<Event<TData>> events,
        ILookup<string, OccurrenceAdjustment<TData>>? adjustments,
        OpenPeriod<DateTimeOffset>? period,
        int? count)
    {
        return events.SelectMany(@event =>
            {
                var adj = adjustments?[@event.Id] ?? [];
                var occurrences = Calculate(@event, adj, period, count);
                return occurrences.Select(x => new { EventId = @event.Id, Occurrence = x });
            })
            .ToLookup(x => x.EventId, x => x.Occurrence);
    }

    private IReadOnlyCollection<Occurrence<TData>> Calculate(
        IEvent @event,
        IEnumerable<OccurrenceAdjustment<TData>> adjustments,
        OpenPeriod<DateTimeOffset>? period,
        int? count = null)
    {
        var adjustmentsArray = adjustments.ToArray();
        var effective = @event.Effective();
        var start = period?.Start ?? EffectiveStartWithAdjustments(effective, adjustmentsArray);
        var end = period?.End ?? EffectiveEndWithAdjustments(effective, adjustmentsArray);

        if (!end.HasValue && !count.HasValue)
        {
            throw new InvalidOperationException(
                $"Unable to calculate open-ended occurrences for event {@event.Id} without count limit");
        }

        var newPeriod = new OpenPeriod<DateTimeOffset>(start, end);

        IEnumerable<Occurrence<TData>> result =
            new OccurrenceCalculator<TData>(newPeriod, [@event, ..adjustmentsArray]).CalculateEnumerable();
        result = period != null ? result.Where(x => period.Intersects(x.Period)) : result;
        result = count.HasValue ? result.Take(count.Value) : result;
        return result.ToArray();
    }

    private static DateTimeOffset EffectiveStartWithAdjustments(
        OpenPeriod<DateTimeOffset> effective,
        IEnumerable<OccurrenceAdjustment<TData>> adjustments)
    {
        return adjustments
            .Where(x => !x.Cancelled && x.MoveTo != null)
            .Select(x => x.MoveTo!.Start)
            .Aggregate(effective.Start, DateTimeOffsetUtil.Min);
    }

    private static DateTimeOffset? EffectiveEndWithAdjustments(
        OpenPeriod<DateTimeOffset> effective,
        IEnumerable<OccurrenceAdjustment<TData>> adjustments)
    {
        if (!effective.End.HasValue)
            return null;

        return adjustments
            .Where(x => !x.Cancelled && x.MoveTo != null)
            .Select(x => x.MoveTo!.End)
            .Aggregate(effective.End.Value, DateTimeOffsetUtil.Max);
    }

    private async Task<ILookup<string, OccurrenceAdjustment<TData>>> FetchAdjustmentsByEventsAsync(
        IEnumerable<IEventEntityBase> entities)
    {
        entities = entities.ToArray();
        var recurrentEventIds = entities.OfType<Event<TData>>().Where(x => x.Recurrence != null).Select(x => x.Id)
            .Distinct().ToArray();

        if (recurrentEventIds.Length == 0)
            return Array.Empty<OccurrenceAdjustment<TData>>().ToLookup(x => x.RecurrentEventId);

        var result = await _eventRepository.GetAllAsync<OccurrenceAdjustment<TData>>(
            FilterRule.In("recurrentEventId", recurrentEventIds));

        return result.ToLookup(x => x.RecurrentEventId);
    }

    public async Task<IReadOnlyCollection<Occurrence<TData>>> OccurrencesAsync(
        IEnumerable<string> ids,
        bool tryCache = false)
    {
        var idInstances = ids.Select(OccurrenceId.Parse).ToArray();
        var entities = await _eventRepository.ByIdAsync<IEventEntityBase>(
            idInstances.Select(x => x.ToString()).Concat(idInstances.Select(x => x.EventId)));
        return idInstances.Select(id => MapOccurrence(id, entities)).Where(x => x != null).ToArray()!;
    }

    private Occurrence<TData>? MapOccurrence(OccurrenceId id, IReadOnlyCollection<IEventEntityBase> entities)
    {
        var adjustment = entities.OfType<OccurrenceAdjustment<TData>>().FirstOrDefault(x => x.Id == id.ToString());

        var @event = entities.OfType<Event<TData>>().FirstOrDefault(x => x.Id == id.EventId);
        @event = @event ?? throw new InvalidOperationException($"Unable to find event for occurrence id {id}");

        if (adjustment != null)
            return OccurrenceCalculator<TData>.TryCalculateOccurrenceAdjustment(@event, adjustment, out var occurrence)
                ? occurrence
                : null;

        return OccurrenceCalculator<TData>.Calculate(id, @event, adjustment);
    }

    private IReadOnlyCollection<Occurrence<TData>> FilterByDataFilterRule(
        IReadOnlyCollection<Occurrence<TData>> result,
        FilterRule dataFilterRule)
    {
        if (_occurrenceFieldMap == null)
            throw new InvalidOperationException($"No field map specified for data type {typeof(TData).FullName}");

        dataFilterRule = dataFilterRule.Replace(new RenameFieldIdFilterRuleVisitor(x => $"{Event.DATA_FIELD}.{x}"));
        return result.AsQueryable().Where(_occurrenceFieldMap, dataFilterRule).ToArray();
    }
}