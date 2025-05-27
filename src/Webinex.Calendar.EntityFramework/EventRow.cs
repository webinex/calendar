namespace Webinex.Calendar.EntityFramework;

public class EventRow<TData> : IEventRow
    where TData : class, ICloneable
{
    public string Id { get; protected set; } = null!;
    public EventRowType Type { get; protected set; }
    public string? RecurrentEventId { get; protected set; }
    public Period<DateTimeOffset> Effective { get; protected set; } = null!;
    public Period<DateTimeOffset> Period { get; protected set; } = null!;
    public string? TimeZone { get; protected set; }
    public EventGroupId Group { get; protected set; } = null!;
    public TData? Data { get; protected set; }
    public bool? Cancelled { get; protected set; }
    public Period<DateTimeOffset>? MoveTo { get; protected set; }

    protected EventRow()
    {
    }

    public EventRow(
        string id,
        EventRowType type,
        string recurrentEventId,
        Period<DateTimeOffset> effective,
        Period<DateTimeOffset> period,
        string? timeZone,
        EventGroupId groupId,
        TData? data = null,
        bool? cancelled = null,
        Period<DateTimeOffset>? moveTo = null)
    {
        Id = id;
        Type = type;
        RecurrentEventId = recurrentEventId;
        Effective = effective.Clone();
        Period = period.Clone();
        TimeZone = timeZone;
        Group = groupId.Clone();
        Data = (TData?)data?.Clone();
        Cancelled = cancelled;
        MoveTo = moveTo?.Clone();
    }

    public static IEventRow From(IEventEntityBase @event)
    {
        switch (@event)
        {
            case Event<TData> eventInstance:
                return From(eventInstance);
            case OccurrenceAdjustment<TData> occurrenceState:
                return From(occurrenceState);
            default:
                throw new ArgumentException($"Unknown event type: {@event.GetType().FullName}", nameof(@event));
        }
    }

    public static EventRow<TData> From(Event<TData> @event)
    {
        return new EventRow<TData>
        {
            Id = @event.Id,
            Type = EventRowType.Event,
            Effective = @event.Period.Clone(),
            Period = @event.Period.Clone(),
            TimeZone = @event.TimeZone,
            Group = @event.Group.Clone(),
            Data = (TData)@event.Data.Clone(),
        };
    }

    public static EventRow<TData> From(OccurrenceAdjustment<TData> state)
    {
        return new EventRow<TData>
        {
            Id = state.Id,
            RecurrentEventId = state.RecurrentEventId,
            Type = EventRowType.Occurrence,
            Effective = state.Effective(),
            Period = state.Period.Clone(),
            MoveTo = state.MoveTo?.Clone(),
            Group = state.Group.Clone(),
            Data = (TData?)state.Data?.Clone(),
            Cancelled = state.Cancelled,
        };
    }

    public IEventEntityBase ToEventEntity()
    {
        return Type switch
        {
            EventRowType.Event => ToEvent(),
            EventRowType.Occurrence => ToOccurrenceState(),
            _ => throw new InvalidOperationException($"Unknown type {Type}"),
        };
    }

    internal Event<TData> ToEvent()
    {
        return new Event<TData>(Id, TimeZone!, Period, Group, Data!);
    }

    internal OccurrenceAdjustment<TData> ToOccurrenceState()
    {
        return new OccurrenceAdjustment<TData>(Id, RecurrentEventId!, Period, MoveTo, Group, Data, Cancelled!.Value);
    }

    public void Apply(IEventEntityBase value)
    {
        switch (value)
        {
            case Event<TData> eventInstance:
                Apply(eventInstance);
                break;
            case OccurrenceAdjustment<TData> occurrenceState:
                Apply(occurrenceState);
                break;
            default:
                throw new ArgumentException($"Unknown value type {value.GetType().FullName}", nameof(value));
        }
    }

    private void Apply(Event<TData> value)
    {
        if (Id != value.Id) throw new InvalidOperationException("Id doesn't match");
        if (Type != EventRowType.Event) throw new InvalidOperationException("Event type doesn't match");

        TimeZone = value.TimeZone;

        if (value.Period != Period)
            Period = value.Period.Clone();

        if (value.Group != Group)
            Group = value.Group.Clone();

        if (value.Data != Data)
            Data = (TData?)value.Data?.Clone();

        if (Effective != value.Period)
            Effective = value.Period.Clone();
    }

    private void Apply(OccurrenceAdjustment<TData> value)
    {
        if (Id != value.Id) throw new InvalidOperationException("Id doesn't match");
        if (Type != EventRowType.Occurrence) throw new InvalidOperationException("Event type doesn't match");
        if (RecurrentEventId != value.RecurrentEventId) throw new InvalidOperationException("RecurrentEventId doesn't match");
        if (Period != value.Period) throw new InvalidOperationException("Period doesn't match");
        if (value.Group != Group) throw new InvalidOperationException("Group doesn't match");
        
        if (Data != value.Data)
            Data = (TData?)value.Data?.Clone();

        if (Cancelled != value.Cancelled)
            Cancelled = value.Cancelled;
        
        if (MoveTo != value.MoveTo)
            MoveTo = value.MoveTo?.Clone();
    }
}