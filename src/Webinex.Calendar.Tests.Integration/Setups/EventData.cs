using Webinex.Calendar.Common;

namespace Webinex.Calendar.Tests.Integration.Setups;

public class EventData : Equatable, ICloneable
{
    protected EventData()
    {
    }

    public EventData(string name, NestedValue? nValue = default)
    {
        Name = name;
        NValue = nValue;
    }

    public string Name { get; protected set; } = null!;
    public NestedValue? NValue { get; protected set; } = null!;

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Name;
        yield return NValue;
    }

    public object Clone()
    {
        return new EventData(Name);
    }

    public class NestedValue : Equatable
    {
        public string Value { get; set; }

        public NestedValue(string value)
        {
            Value = value;
        }

        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return Value;
        }
    }
}