namespace Webinex.Calendar;

public class Occurrence<TData> : IEventEntityBase
    where TData : class, ICloneable
{
    public string Id { get; }
    public Period<DateTimeOffset> Period { get; }
    public EventGroupId Group { get; }
    public TData Data { get; }

    public Occurrence(
        string id,
        EventGroupId groupId,
        Period<DateTimeOffset> period,
        TData data)
    {
        Id = id;
        Period = period;
        Data = data;
        Group = groupId;
    }
}