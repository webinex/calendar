using Webinex.Asky;
using Webinex.Calendar.Calculators;

namespace Webinex.Calendar.Services;

internal interface IOccurrenceReadService<TData>
    where TData : class, ICloneable
{
    Task<IReadOnlyCollection<Occurrence<TData>>> OccurrencesAsync(
        Period<DateTimeOffset> period,
        FilterRule? dataFilterRule = null,
        bool tryCache = false);

    Task<IReadOnlyCollection<Occurrence<TData>>> MaterializedOccurrencesAsync(
        FilterRule? filterRule = null,
        IEnumerable<SortRule>? sortRules = null,
        PagingRule? pagingRule = null);

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

    public async Task<IReadOnlyCollection<Occurrence<TData>>> MaterializedOccurrencesAsync(
        FilterRule? filterRule = null,
        IEnumerable<SortRule>? sortRules = null,
        PagingRule? pagingRule = null)
    {
        var rows = await _eventRepository.GetAllAsync(
            EventEntityType.OneTimeEvent | EventEntityType.OccurrenceAdjustment,
            filterRule,
            sortRules, pagingRule);

        return Map().ToArray();

        IEnumerable<Occurrence<TData>> Map()
        {
            foreach (var entity in rows)
                switch (entity)
                {
                    case Event<TData> @event:
                        yield return OccurrenceCalculator<TData>.CalculateOneTime(@event);
                        break;
                    case OccurrenceAdjustment<TData> adjustment:
                        if (OccurrenceCalculator<TData>.TryCalculateOccurrenceAdjustment(adjustment, out var occurrence))
                            yield return occurrence;
                        break;
                    default:
                        throw new InvalidOperationException(
                            $"Unexpected entity type {entity.GetType().FullName} for materialized occurrences");
                }
        }
    }

    public async Task<IReadOnlyCollection<Occurrence<TData>>> OccurrencesAsync(
        IEnumerable<string> ids,
        bool tryCache = false)
    {
        var idInstances = ids.Select(OccurrenceId.Parse).ToArray();
        var entities = await _eventRepository.ByIdAsync<IEventEntityBase>(
            idInstances.Select(x => x.ToString()).Concat(idInstances.Select(x => x.EventId)));
        return idInstances.Select(id => MapOccurrence(id, entities)).ToArray();
    }

    private Occurrence<TData> MapOccurrence(OccurrenceId id, IReadOnlyCollection<IEventEntityBase> entities)
    {
        var adjustment = entities.OfType<OccurrenceAdjustment<TData>>().FirstOrDefault(x => x.Id == id.ToString());

        if (adjustment != null &&
            OccurrenceCalculator<TData>.TryCalculateOccurrenceAdjustment(adjustment, out var result1))
            return result1;

        var @event = entities.OfType<Event<TData>>().FirstOrDefault(x => x.Id == id.EventId);
        @event = @event ?? throw new InvalidOperationException($"Unable to find event for occurrence id {id}");

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