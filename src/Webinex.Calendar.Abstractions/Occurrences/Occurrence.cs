namespace Webinex.Calendar;

public class Occurrence<TData> : IEventEntityBase
    where TData : class
{
    public string Id { get; }
    public Period<DateTimeOffset> Period { get; }
    public EventGroup Group { get; }
    public TData Data { get; }

    public Occurrence(
        string id,
        EventGroup group,
        Period<DateTimeOffset> period,
        TData data)
    {
        Id = id;
        Period = period;
        Data = data;
        Group = group;
    }
}