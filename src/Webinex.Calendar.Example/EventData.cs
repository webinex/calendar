using Webinex.Calendar.Common;

namespace Webinex.Calendar.Example;

public class EventData : Equatable, ICloneable
{
    protected EventData()
    {
    }
    
    public EventData(string title)
    {
        Title = title;
    }

    public string Title { get; protected set; } = null!;

    public static bool operator ==(EventData? left, EventData? right)
    {
        return EqualOperator(left, right);
    }

    public static bool operator !=(EventData? left, EventData? right)
    {
        return NotEqualOperator(left, right);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Title;
    }

    public object Clone()
    {
        return new EventData(Title);
    }
}