namespace Webinex.Calendar.Events;

public interface IEvent
{
    EventType Type { get; }
}