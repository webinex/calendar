namespace Webinex.Calendar;

public static class EventId
{
    public static string New(EventType eventType, byte[]? id = null)
    {
        id ??= Guid.NewGuid().ToByteArray();
        
        var prefix = eventType switch
        {
            EventType.Recurrent => "R",
            EventType.OneTime => "O",
            _ => throw new ArgumentOutOfRangeException(nameof(eventType), eventType, null)
        };

        return prefix + Convert.ToBase64String(id);
    }

    public static EventType TypeOf(string id)
    {
        return id[0] switch
        {
            'R' => EventType.Recurrent,
            'O' => EventType.OneTime,
            _ => throw new ArgumentOutOfRangeException(nameof(id), id, null)
        };
    }

    public static bool IsOneTime(string id) => TypeOf(id) == EventType.OneTime;
    public static bool IsRecurrent(string id) => TypeOf(id) == EventType.Recurrent;
}