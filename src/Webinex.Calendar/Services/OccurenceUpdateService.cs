using Webinex.Calendar.Common;
using Webinex.Calendar.Extensions;
using Webinex.Coded;

namespace Webinex.Calendar.Services;

internal interface IOccurrenceUpdateService<TData>
{
    Task<IReadOnlyCollection<IEventEntityBase>> UpdateOccurrenceRangeAsync(
        IEnumerable<UpdateOccurrenceArgs<TData>> args);
}

internal class OccurrenceUpdateService<TData> : IOccurrenceUpdateService<TData>
    where TData : class, ICloneable
{
    private readonly IEventRepository<TData> _eventRepository;
    private readonly RecurrentEventUpdateService<TData> _recurrentEventUpdateService;

    public OccurrenceUpdateService(
        IEventRepository<TData> eventRepository,
        RecurrentEventUpdateService<TData> recurrentEventUpdateService)
    {
        _eventRepository = eventRepository;
        _recurrentEventUpdateService = recurrentEventUpdateService;
    }

    public async Task<IReadOnlyCollection<IEventEntityBase>> UpdateOccurrenceRangeAsync(
        IEnumerable<UpdateOccurrenceArgs<TData>> args)
    {
        var operations = await MapUpdateOperationsAsync(args.ToArray()).ToArrayAsync();
        var result = await _eventRepository.PatchAsync(operations.SelectMany(x => x));
        return result.Select(x => x.Value).ToArray();
    }

    private async IAsyncEnumerable<IEnumerable<Operation>> MapUpdateOperationsAsync(UpdateOccurrenceArgs<TData>[] args)
    {
        foreach (var arg in args)
        {
            switch (EventId.TypeOf(arg.Id.EventId))
            {
                case EventType.OneTime:
                    yield return await MapUpdateOneTimeOccurrenceOperationsAsync(arg).ToArrayAsync();
                    break;
                case EventType.Recurrent when arg.Behavior == OccurenceUpdateBehavior.Occurrence:
                    yield return await MapUpdateRecurrentOccurrenceOperationsAsync(arg);
                    break;
                case EventType.Recurrent when arg.Behavior == OccurenceUpdateBehavior.Group:
                    yield return await _recurrentEventUpdateService.MapOperationsAsync(arg);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(arg), $"{arg.Id}, {arg.Behavior}, {arg.Period}")
                    {
                        Data = { ["Data"] = arg.Data }
                    };
            }
        }
    }

    private async IAsyncEnumerable<Operation> MapUpdateOneTimeOccurrenceOperationsAsync(UpdateOccurrenceArgs<TData> arg)
    {
        var @event = await _eventRepository.EventOrThrowAsync(arg.Id.EventId);

        if (arg.Data != null)
            @event.SetData(
                arg.Data?.Value ??
                throw new InvalidOperationException($"Unable to reset data for one time event {arg.Id}"));

        if (arg.Period != null)
            @event.SetPeriod(arg.Period.Value);

        yield return Operation.Update(@event);
    }

    private async Task<IEnumerable<Operation>> MapUpdateRecurrentOccurrenceOperationsAsync(
        UpdateOccurrenceArgs<TData> arg)
    {
        var occurrenceAdjustment = await _eventRepository.OccurrenceAdjustmentAsync(arg.Id.ToString());
        return occurrenceAdjustment != null
            ? MapUpdateExistingRecurrentOccurrenceOperations(arg, occurrenceAdjustment)
            : await MapUpdateNotExistingRecurrentOccurrenceOperationsAsync(arg);
    }

    private IEnumerable<Operation> MapUpdateExistingRecurrentOccurrenceOperations(
        UpdateOccurrenceArgs<TData> arg,
        OccurrenceAdjustment<TData> occurrenceAdjustment)
    {
        if (arg.Period != null) occurrenceAdjustment.Move(arg.Period.Value);
        if (arg.Data != null) occurrenceAdjustment.SetData(arg.Data?.Value);
        yield return Operation.Update(occurrenceAdjustment);
    }

    private async Task<IEnumerable<Operation>> MapUpdateNotExistingRecurrentOccurrenceOperationsAsync(
        UpdateOccurrenceArgs<TData> arg)
    {
        if (arg.Data == null && arg.Period == null)
            return [];

        var recurrentEvent = await _eventRepository.EventAsync(arg.Id.EventId) ??
                             throw CodedException.NotFound(arg.Id.EventId);

        var newOccurrenceAdjustment = OccurrenceAdjustment<TData>.NewUpdate(
            arg.Id,
            Period.New(arg.Id.Start, arg.Id.Start.Add(recurrentEvent.Duration())),
            recurrentEvent.Group,
            arg.Data?.Value,
            arg.Period?.Value);

        return [Operation.Add(newOccurrenceAdjustment)];
    }
}