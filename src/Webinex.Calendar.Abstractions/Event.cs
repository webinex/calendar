namespace Webinex.Calendar;

public static class Event
{
    public static readonly string DATA_FIELD = "data";
    
    public static readonly EventFactory Factory = new();

    public static Event<TData> New<TData>(
        Period<DateTimeOffset> period,
        string timeZone,
        TData data,
        Recurrence? recurrence = null,
        string? id = null,
        EventGroupId? group = null)
        where TData : class, ICloneable
    {
        return Event<TData>.New(period, timeZone, data, recurrence, id, group);
    }
}