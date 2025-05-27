using Webinex.Asky;
using Webinex.Calendar.Common;
using Webinex.Calendar.Extensions;
using Webinex.Coded;

namespace Webinex.Calendar.Services;

internal interface IOccurrenceCancellationService<TData>
{
    Task CancelRangeAsync(IEnumerable<CancelOccurrenceArgs> args);
}

internal class OccurrenceCancellationService<TData> : IOccurrenceCancellationService<TData>
    where TData : class, ICloneable
{
    private readonly IEventRepository<TData> _eventRepository;

    public OccurrenceCancellationService(IEventRepository<TData> eventRepository)
    {
        _eventRepository = eventRepository;
    }

    public async Task CancelRangeAsync(IEnumerable<CancelOccurrenceArgs> args)
    {
        var operations = await MapCancelOperations(args.ToArray()).ToArrayAsync();
        await _eventRepository.PatchAsync(operations.SelectMany(x => x));
    }

    private async IAsyncEnumerable<IEnumerable<Operation>> MapCancelOperations(CancelOccurrenceArgs[] args)
    {
        foreach (var arg in args)
        {
            var id = OccurrenceId.Parse(arg.Id);
            yield return arg.Behavior == OccurenceUpdateBehavior.Group && EventId.TypeOf(id.EventId) == EventType.Recurrent
                ? await MapCancelGroupOperationsAsync(id)
                : await MapCancelOccurrenceOperationsAsync(id);
        }
    }

    /*
     * Whenever you cancel ongoing occurrence, recurrent event might be updated to complete on a date of occurrence
     * and all upcoming occurrence adjustments and recurrent events of the same group might be removed from database
     */
    private async Task<IEnumerable<Operation>> MapCancelGroupOperationsAsync(OccurrenceId id)
    {
        var @event = await _eventRepository.EventAsync(id.EventId) ?? throw CodedException.NotFound(id.EventId);
        var events = await _eventRepository.GetAllAsync<IEventEntityBase>(FilterRule.Eq("group.id", @event.Group.Id));
        var eventOperation = MapEndUntilOccurenceRecurrenceEventOperation(@event, id);
        
        return events
            .Where(x => x.Period.Start >= id.Start)
            .Select(Operation.Remove)
            .Concat([eventOperation]).ToArray();
    }

    /**
     * When occurence is first occurence of recurrent event, it might be deleted
     * otherwise it might be ended a date before occurence
     */
    private Operation MapEndUntilOccurenceRecurrenceEventOperation(Event<TData> @event, OccurrenceId id)
    {
        var endDateNew = id.Start.ToDateOnly(@event.TimeZone).AddDays(-1);
        
        if (endDateNew < @event.Recurrence!.StartDate())
            return Operation.Remove(@event);
        
        @event.SetEndDate(endDateNew);
        return Operation.Update(@event);
    }

    /*
     * Occurrence cancel behavior difference based on the current state:
     * * If occurrence adjustment already exists, it might be updated
     * * If occurrence adjustment doesn't exist, it might be created
     * * If occurrence matches with one time event, one time event state might be deleted
     */
    private async Task<IEnumerable<Operation>> MapCancelOccurrenceOperationsAsync(OccurrenceId id)
    {
        return EventId.TypeOf(id.EventId) switch
        {
            EventType.OneTime => await MapDeleteOneTimeEventOperationAsync(id),
            EventType.Recurrent => await MapCancelRecurrentOccurrenceOperationsAsync(id),
            _ => throw new InvalidOperationException($"Unexpected event id: {id}"),
        };
    }

    private async Task<IEnumerable<Operation>> MapDeleteOneTimeEventOperationAsync(OccurrenceId id)
    {
        var @event = await _eventRepository.EventAsync(id.EventId)
                     ?? throw CodedException.NotFound(id.EventId);
        return [Operation.Remove(@event)];
    }

    private async Task<IEnumerable<Operation>> MapCancelRecurrentOccurrenceOperationsAsync(OccurrenceId id)
    {
        var occurrenceAdjustment = await _eventRepository.OccurrenceAdjustmentAsync(id.ToString());

        if (occurrenceAdjustment != null)
        {
            occurrenceAdjustment.Cancel();
            return [Operation.Update(occurrenceAdjustment)];
        }

        var recurrentEvent = await _eventRepository.EventAsync(id.EventId) ?? throw CodedException.NotFound(id.EventId);
        var newOccurrenceAdjustment = OccurrenceAdjustment<TData>.NewCancel(
            recurrentEvent.Id,
            recurrentEvent.Group,
            Period.New(id.Start, id.Start.Add(recurrentEvent.Duration())));
        return [Operation.Add(newOccurrenceAdjustment)];
    }
}