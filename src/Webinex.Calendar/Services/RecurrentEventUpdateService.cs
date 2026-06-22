using Webinex.Asky;
using Webinex.Calendar.Common;
using Webinex.Calendar.Extensions;
using Webinex.Coded;

namespace Webinex.Calendar.Services;

internal class RecurrentEventUpdateService<TData>
    where TData : class, ICloneable
{
    private readonly IEventRepository<TData> _eventRepository;

    public RecurrentEventUpdateService(IEventRepository<TData> eventRepository)
    {
        _eventRepository = eventRepository;
    }

    public async Task<IEnumerable<Operation>> MapOperationsAsync(UpdateOccurrenceArgs<TData> args)
    {
        var parentEvent = await _eventRepository.EventAsync(args.Id.EventId)
                          ?? throw CodedException.NotFound(args.Id.EventId);

        var groupEvents = await _eventRepository.GetAllAsync<IEventEntityBase>(
            FilterRule.Eq("group.id", parentEvent.Group.Id));

        var adjustment = (OccurrenceAdjustment<TData>?)groupEvents.FirstOrDefault(x => x.Id == args.Id.ToString());

        var operations = new OperationCalculator(args, parentEvent, adjustment, groupEvents.ToArray()).Calculate();
        return operations;
    }

    /*
     * Whenever you update occurrence for a recurrent group, current recurrent event might be ended
     * at the date before occurrence and new one might be created.
     * All subsequent occurrence adjustments and recurrent events might be deleted
     */
    private record OperationCalculator(
        UpdateOccurrenceArgs<TData> Args,
        Event<TData> ParentEvent,
        OccurrenceAdjustment<TData>? Adjustment,
        IEventEntityBase[] GroupEvents)
    {
        private Period<DateTimeOffset> NewEventPeriod => Args.Period?.Value ?? Adjustment?.Period ?? Period.New(
            Args.Id.Start,
            Args.Id.Start.Add(ParentEvent.Duration()));

        private TimeSpan NewEventGroupOffset => NewEventPeriod.Start - Args.Id.Start + ParentEvent.Group.OffsetTimeSpan();
        private EventGroupId NewEventGroupId => new(ParentEvent.Group.Id, NewEventGroupOffset);
        private string NewEventTimeZone => Args.TimeZone?.Value ?? ParentEvent.TimeZone;
        private TData NewEventData => Args.Data?.Value ?? Adjustment?.Data ?? ParentEvent.Data;
        private DateOnly NewEventStartDate => NewEventPeriod.Start.ToDateOnly(NewEventTimeZone);
        private DateOnly? NewEventEndDate => EndDateOfAffectedRecurrentEvents();

        public IEnumerable<Operation> Calculate()
        {
            return MapEventUpdateOperations().Concat(MapDeleteSubsequentEventEntitiesOperations()).ToArray();
        }

        private IEnumerable<Operation> MapEventUpdateOperations()
        {
            if (IsFirstOccurence())
                return [MapUpdateParentEventOperation()];

            return [MapAddNewEventOperation(), MapEndParentEventOperation()];
        }

        private IEnumerable<Operation> MapDeleteSubsequentEventEntitiesOperations()
        {
            var toDelete = GroupEvents.Where(x => x.Period.Start >= Args.Id.Start && x.Id != ParentEvent.Id).ToArray();
            return toDelete.Select(Operation.Remove).ToArray();
        }

        private Operation MapUpdateParentEventOperation()
        {
            if (Args.Period != null)
                ParentEvent.SetPeriod(Args.Period.Value);

            if (Args.TimeZone != null)
                ParentEvent.SetTimeZone(Args.TimeZone.Value);

            if (Args.Recurrence != null)
                ParentEvent.SetRecurrence(Args.Recurrence.Value);

            if (Args.Data != null)
                ParentEvent.SetData(
                    Args.Data.Value ?? throw new InvalidOperationException(
                        $"Unexpected null {nameof(Args.Data)} for recurrent event update args"));

            return Operation.Update(ParentEvent);
        }

        private Operation MapAddNewEventOperation()
        {
            var recurrence = Args.Recurrence?.Value ?? ParentEvent.Recurrence!.WithPeriod(
                NewEventStartDate,
                NewEventEndDate);

            var newEvent = Event<TData>.New(
                NewEventPeriod,
                NewEventTimeZone,
                NewEventData,
                recurrence,
                group: NewEventGroupId);

            return Operation.Add(newEvent);
        }

        private Operation MapEndParentEventOperation()
        {
            ParentEvent.SetEndDate(NewEventStartDate.AddDays(-1));
            return Operation.Update(ParentEvent);
        }

        private bool IsFirstOccurence()
        {
            return ParentEvent.Recurrence!.StartDate() == Args.Id.Start.ToDateOnly(ParentEvent.TimeZone);
        }

        private DateOnly? EndDateOfAffectedRecurrentEvents()
        {
            var affectedRecurrentEvents = GroupEvents
                .OfType<Event<TData>>()
                .Where(x => x.Id == ParentEvent.Id || x.Period.Start >= Args.Id.Start)
                .ToArray();

            return affectedRecurrentEvents.Any(x => !x.Recurrence!.EndDate().HasValue)
                ? null
                : affectedRecurrentEvents.Max(x => x.Recurrence!.EndDate());
        }
    }
}
