namespace Webinex.Calendar.Common;

public abstract class EnumValue<TValue> : ValueObject, ISingleValueObject<TValue> where TValue : notnull
{
    protected abstract HashSet<TValue> PossibleValues { get; }

    public TValue Value { get; protected init; } = default!;

    protected EnumValue()
    {
    }

    protected EnumValue(TValue value)
    {
        Value = value;
    }

    TValue ISingleValueObject<TValue>.Convert() => Value;

    public override string ToString() => Value.ToString()!;

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }
}