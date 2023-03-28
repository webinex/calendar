using Webinex.Calendar.Common;

namespace Webinex.Calendar.Tests.Integration.Setups;

public class EventData : Equatable, ICloneable
{
    protected EventData()
    {
    }
    
    public EventData(string name)
    {
        Name = name;
    }

    public string Name { get; protected set; } = null!;

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
        yield return Name;
    }

    public object Clone()
    {
        return new EventData(Name);
    }
}