namespace Webinex.Calendar.EntityFramework;

internal static class EventRowTypeUtil
{
    public static EventRowType? ByType<T>()
        where T : IEventEntityBase
    {
        if (typeof(T) == typeof(IEventEntityBase))
            return null;

        if (typeof(T).IsGenericType && typeof(T).GetGenericTypeDefinition() == typeof(Event<>))
            return EventRowType.Event;

        if (typeof(T).IsGenericType && typeof(T).GetGenericTypeDefinition() == typeof(OccurrenceAdjustment<>))
            return EventRowType.Occurrence;

        throw new InvalidOperationException($"Type {typeof(T).FullName} is unknown");
    }
}