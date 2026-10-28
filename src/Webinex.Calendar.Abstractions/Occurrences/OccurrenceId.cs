using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Webinex.Calendar;

public class OccurrenceId : Equatable
{
    public string EventId { get; protected set; } = null!;

    /// <summary>
    ///     Start of recurrent event occurrence. Null for one time event occurrence,
    ///     so its id doesn't change when event moved.
    ///     Ids of one time events created before might contain start, they are still valid.
    /// </summary>
    public DateTimeOffset? Start { get; protected set; }
    
    /// <summary>
    ///     Returns true if occurrence is one time event occurrence, false if recurrent event occurrence.
    /// </summary>
    public bool IsOneTime() => Calendar.EventId.IsOneTime(EventId);
    
    /// <summary>
    ///     Returns true if occurrence is recurrent event occurrence, false if one time event occurrence.
    /// </summary>
    [MemberNotNullWhen(true, nameof(Start))]
    public bool IsRecurrent() => Calendar.EventId.IsRecurrent(EventId);

    private OccurrenceId(string eventId, DateTimeOffset? start)
    {
        EventId = eventId ?? throw new ArgumentNullException(nameof(eventId));
        Start = start?.ToUniversalTime();
    }

    public static OccurrenceId OneTime(string eventId)
    {
        if (!Calendar.EventId.IsOneTime(eventId))
            throw new ArgumentException($"Event id {eventId} is not one time event id", nameof(eventId));

        return new(eventId, null);
    }

    public static OccurrenceId Recurrent(string eventId, DateTimeOffset occurrenceStart)
    {
        if (!Calendar.EventId.IsRecurrent(eventId))
            throw new ArgumentException($"Event id {eventId} is not recurrent event id", nameof(eventId));
        
        if (occurrenceStart == default)
            throw new ArgumentException($"Occurrence start is required for recurrent event id {eventId}", nameof(occurrenceStart));

        return new(eventId, occurrenceStart.ToUniversalTime());
    }

    internal static OccurrenceId UnsafeLegacyOneTimeShouldNotBeUsed(string eventId, DateTimeOffset start)
    {
        if (!Calendar.EventId.IsOneTime(eventId))
            throw new ArgumentException($"Event id {eventId} is not one time event id", nameof(eventId));

        return new(eventId, start.ToUniversalTime());
    }

    /// <summary>
    ///     Start of recurrent event occurrence. Throws when start is null.
    /// </summary>
    public DateTimeOffset GetStartOrThrow() =>
        Start ?? throw new InvalidOperationException($"Occurrence id {this} doesn't contain start");

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
        // Zero ticks means no start, format kept the same to be compatible with previously issued ids
        DateTimeOffset? start = ticks == 0 ? null : new DateTimeOffset(ticks, TimeSpan.Zero);
        var eventId = Encoding.ASCII.GetString(eventIdBytes);
        
        if (Calendar.EventId.IsRecurrent(eventId) && !start.HasValue)
            throw new InvalidOperationException($"Occurrence id {value} is recurrent but doesn't contain start");
        
        return new OccurrenceId(eventId, start);
    }

    public override string ToString()
    {
        var ticksBytes = new byte[8];
        BitConverter.GetBytes(Start?.UtcTicks ?? 0).CopyTo(ticksBytes, 0);
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