namespace Webinex.Calendar;

public interface IEvent : IEventEntityBase
{
    string TimeZone { get; }
    Recurrence? Recurrence { get; }
}

public interface IEvent<TData> : IEvent
{
    TData Data { get; }
}