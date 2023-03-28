using Webinex.Calendar.Common;

namespace Webinex.Calendar.Events;

public class RecurrentEventStateId : Equatable
{
    public RecurrentEventStateId(Guid recurrentEventId, DateTimeOffset eventStart)
    {
        RecurrentEventId = recurrentEventId;
        EventStart = eventStart;
    }

    public Guid RecurrentEventId { get; }
    public DateTimeOffset EventStart { get; }

    public static bool operator ==(RecurrentEventStateId? left, RecurrentEventStateId? right)
    {
        return EqualOperator(left, right);
    }

    public static bool operator !=(RecurrentEventStateId? left, RecurrentEventStateId? right)
    {
        return NotEqualOperator(left, right);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return RecurrentEventId;
        yield return EventStart;
    }
}