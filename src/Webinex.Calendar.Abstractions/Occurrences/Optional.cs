namespace Webinex.Calendar;

public class Optional<TValue>
{
    public TValue Value { get; protected set; }

    public Optional(TValue value)
    {
        Value = value;
    }

    public override string ToString()
    {
        return Value?.ToString() ?? string.Empty;
    }
}