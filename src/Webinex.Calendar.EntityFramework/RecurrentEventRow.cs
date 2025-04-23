using Webinex.Calendar.Extensions;

namespace Webinex.Calendar.EntityFramework;

public class RecurrentEventRow<TData> : IEventRow
    where TData : class, ICloneable
{
    public string Id { get; protected set; } = null!;
    public EventGroup Group { get; protected set; } = null!;
    public OpenPeriod<DateTimeOffset> Effective { get; protected set; } = null!;
    public Period<DateTimeOffset> Period { get; protected set; } = null!;
    public string TimeZone { get; protected set; } = null!;
    public TData Data { get; protected set; } = null!;
    public Recurrence Recurrence { get; protected set; } = null!;

    protected RecurrentEventRow()
    {
    }

    public RecurrentEventRow(
        string id,
        EventGroup group,
        OpenPeriod<DateTimeOffset> effective,
        Period<DateTimeOffset> period,
        string timeZone,
        TData data,
        Recurrence recurrence)
    {
        Id = id;
        Group = group.Clone();
        Effective = effective.Clone();
        Period = period.Clone();
        TimeZone = timeZone;
        Data = (TData)data.Clone();
        Recurrence = recurrence.Clone();
    }

    public static RecurrentEventRow<TData> From(Event<TData> @event)
    {
        @event = @event ?? throw new ArgumentNullException(nameof(@event));

        if (@event.Recurrence == null)
            throw new InvalidOperationException(
                $"Unable to create recurrent event row for non recurrent event {@event.Id}");

        return new RecurrentEventRow<TData>
        {
            Id = @event.Id,
            Effective = @event.Effective(),
            Period = @event.Period.Clone(),
            TimeZone = @event.TimeZone,
            Group = @event.Group.Clone(),
            Data = (TData)@event.Data.Clone(),
            Recurrence = @event.Recurrence.Clone(),
        };
    }

    internal Event<TData> ToEvent()
    {
        return new Event<TData>(Id, TimeZone!, Period, Group, Data, Recurrence);
    }

    IEventEntityBase IEventRow.ToEventEntity() => ToEvent();

    public void Apply(IEventEntityBase entityBase)
    {
        if (entityBase is not Event<TData> value)
            throw new InvalidOperationException($"Expected only {nameof(Event<TData>)}");

        if (Id != value.Id) throw new InvalidOperationException("Id doesn't match");
        if (value.Recurrence == null)
            throw new InvalidOperationException(
                $"Unable to convert recurrent event row to non recurrent for event {value.Id}");

        TimeZone = value.TimeZone;

        if (value.Period != Period)
            Period = value.Period.Clone();

        if (value.Group != Group)
            Group = value.Group.Clone();

        if (value.Data != Data)
            Data = (TData)value.Data.Clone();

        if (value.Recurrence != Recurrence)
            Recurrence = value.Recurrence.Clone();

        var newEffective = value.Effective();
        if (Effective != newEffective)
            Effective = newEffective.Clone();
    }
}