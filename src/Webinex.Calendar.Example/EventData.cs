﻿using Webinex.Calendar.Common;

namespace Webinex.Calendar.Example;

public class EventData : ValueObject, ICloneable
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