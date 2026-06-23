using Webinex.Asky;

namespace Webinex.Calendar.EntityFramework;

internal static class EventUtil
{
    public static IEventRow ToRow<TData>(IEventEntityBase eventEntity)
        where TData : class, ICloneable
    {
        return eventEntity switch
        {
            Event<TData> @event when @event.Recurrence != null => RecurrentEventRow<TData>.From(@event),
            _ => EventRow<TData>.From(eventEntity),
        };
    }
}