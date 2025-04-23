
namespace Webinex.Calendar.Common;

public enum OperationType
{
    Add,
    Update,
    Remove
}

public class Operation
{
    public OperationType Type { get; }
    public IEventEntityBase Value { get; }

    public Operation(OperationType type, IEventEntityBase value)
    {
        Type = type;
        Value = value;
    }

    public static Operation Add<T>(T value)
        where T : IEventEntityBase
    {
        return new Operation(OperationType.Add, value);
    }

    public static Operation Update<T>(T value)
        where T : IEventEntityBase
    {
        return new Operation(OperationType.Update, value);
    }

    public static Operation Remove<T>(T value)
        where T : IEventEntityBase
    {
        return new Operation(OperationType.Remove, value);
    }
}
