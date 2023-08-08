using Webinex.Calendar.Common;
using Webinex.Calendar.Events;

namespace Webinex.Calendar;

internal class EventRowId : Equatable
{
    public EventRowId(
        EventType type,
        Guid id,
        Guid? recurrentEventStateRecurrentEventId,
        DateTimeOffset? recurrentEventStateEffectiveStart)
    {
        Type = type;
        Id = id;
        RecurrentEventStateRecurrentEventId = recurrentEventStateRecurrentEventId;
        RecurrentEventStateEffectiveStart = recurrentEventStateEffectiveStart;
    }

    public EventType Type { get; }
    public Guid? Id { get; }
    public Guid? RecurrentEventStateRecurrentEventId { get; }
    public DateTimeOffset? RecurrentEventStateEffectiveStart { get; }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Type;
        yield return Id;
        yield return RecurrentEventStateRecurrentEventId;
        yield return RecurrentEventStateEffectiveStart;
    }
}