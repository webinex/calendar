using System.Text;

namespace Webinex.Calendar;

public class OccurrenceId : Equatable
{
    public string EventId { get; protected set; } = null!;
    public DateTimeOffset Start { get; protected set; }

    public OccurrenceId(string eventId, DateTimeOffset start)
    {
        EventId = eventId ?? throw new ArgumentNullException(nameof(eventId));
        Start = start.ToUniversalTime();
    }

    protected OccurrenceId()
    {
    }

    public static OccurrenceId Parse(string value)
    {
        value = value.Substring(1).Replace('.', '/');
        var bytes = Convert.FromBase64String(value);
        var ticksBytes = bytes.TakeLast(8).ToArray();
        var eventIdBytes = bytes.Take(bytes.Length - 8).ToArray();
        var ticks = BitConverter.ToInt64(ticksBytes, 0);
        var start = new DateTimeOffset(ticks, TimeSpan.Zero);
        var eventId = Encoding.ASCII.GetString(eventIdBytes);
        return new OccurrenceId(eventId, start);
    }

    public override string ToString()
    {
        var ticksBytes = new byte[8];
        BitConverter.GetBytes(Start.UtcTicks).CopyTo(ticksBytes, 0);
        var bytes = Encoding.ASCII.GetBytes(EventId).Concat(ticksBytes).ToArray();
        return "O" + Convert.ToBase64String(bytes).Replace('/', '.');
    }

    public static bool IsValid(string id)
    {
        return id.StartsWith("O");
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return EventId;
        yield return Start;
    }
}