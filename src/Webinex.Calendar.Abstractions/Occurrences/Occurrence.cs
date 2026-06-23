namespace Webinex.Calendar;

public class Occurrence<TData> : IEventEntityBase
    where TData : class, ICloneable
{
    /// <summary>
    ///     Occurrence identifier. Serialized value of <see cref="OccurrenceId"/>.
    /// </summary>
    public string Id { get; }
    
    /// <summary>
    ///     Period of occurrence in UTC.
    /// </summary>
    public Period<DateTimeOffset> Period { get; }
    
    /// <summary>
    ///     Time zone of original event.
    /// </summary>
    public string TimeZone { get; }
    
    /// <summary>
    ///     Group of occurrence. Equals to <see cref="Event{TData}.Group"/> of original event.
    /// </summary>
    public EventGroupId Group { get; }
    
    /// <summary>
    ///     Data of occurrence. Equals to <see cref="Event{TData}.Data"/> of original event if no adjustments applied,
    ///     otherwise equals to data of last applied adjustment.
    /// </summary>
    public TData Data { get; }

    public bool IsOneTime() => OccurrenceId.Parse(Id).IsOneTimeEvent();

    public Occurrence(
        string id,
        string timeZone,
        EventGroupId groupId,
        Period<DateTimeOffset> period,
        TData data)
    {
        Id = id;
        TimeZone = timeZone;
        Period = period.ToUtc();
        Data = data;
        Group = groupId;
    }
}