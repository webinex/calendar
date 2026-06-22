using System.Collections;
using System.Runtime.CompilerServices;

namespace Webinex.Calendar;

public class Arg<T>
{
    public string Name { get; }
    public T Value { get; }

    public Arg(string name, T value)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Value = value;
    }

    public ArgumentException Throw(string message)
    {
        throw new ArgumentException(message, Name);
    }
}

public static class ArgExtensions
{
    public static Arg<T> NotNull<T>(this Arg<T> arg)
    {
        if (arg.Value == null)
            arg.Throw("Might not be null");

        return arg;
    }

    public static Arg<string?> ASCII(this Arg<string?> arg)
    {
        if (arg.Value != null && !arg.Value.All(c => c <= 127))
            throw arg.Throw("Might be ASCII string");

        return arg;
    }

    public static Arg<T> Lt<T>(this Arg<T> arg, T maxValue)
        where T : IComparable<T>
    {
        if (arg.Value.CompareTo(maxValue) >= 0)
            throw arg.Throw($"Might be less than {maxValue}");

        return arg;
    }

    public static Arg<T?> Lt<T>(this Arg<T?> arg, T maxValue)
        where T : struct, IComparable<T>
    {
        if (arg.Value.HasValue && arg.Value.Value.CompareTo(maxValue) >= 0)
            throw arg.Throw($"Might be less than {maxValue}");

        return arg;
    }

    public static Arg<T> Gt<T>(this Arg<T> arg, T maxValue)
        where T : IComparable<T>
    {
        if (arg.Value.CompareTo(maxValue) <= 0)
            throw arg.Throw($"Might be greater than {maxValue}");

        return arg;
    }

    public static Arg<T?> Gt<T>(this Arg<T?> arg, T maxValue)
        where T : struct, IComparable<T>
    {
        if (arg.Value.HasValue && arg.Value.Value.CompareTo(maxValue) <= 0)
            throw arg.Throw($"Might be greater than {maxValue}");

        return arg;
    }
}

internal class GuardChain
{
    public GuardChain NotNull<T>(T? value, [CallerArgumentExpression("value")] string name = "")
        where T : class
    {
        if (value == null)
            throw new ArgumentException("Value might not be null", name);

        return this;
    }

    public GuardChain NotNull<T>(T? value, [CallerArgumentExpression("value")] string name = "")
        where T : struct
    {
        if (!value.HasValue)
            throw new ArgumentException("Value might not be null", name);

        return this;
    }

    public GuardChain Null<T>(T? value, [CallerArgumentExpression("value")] string name = "")
        where T : class
    {
        if (value != null)
            throw new ArgumentException("Value might be null", name);

        return this;
    }

    public GuardChain Null<T>(T? value, [CallerArgumentExpression("value")] string name = "")
        where T : struct
    {
        if (value.HasValue)
            throw new ArgumentException("Value might be null", name);

        return this;
    }

    public GuardChain Gte<T>(T? value, T than, [CallerArgumentExpression("value")] string name = "")
        where T : struct, IComparable<T>
    {
        if (value == null)
            return this;
        
        if (value.Value.CompareTo(than) < 0)
            throw new ArgumentException($"Value might be greater than or equal to {than}", name);

        return this;
    }

    public GuardChain Lte<T>(T? value, T than, [CallerArgumentExpression("value")] string name = "")
        where T : struct, IComparable<T>
    {
        if (value == null)
            return this;
        
        if (value.Value.CompareTo(than) > 0)
            throw new ArgumentException($"Value might be less than or equal to {than}", name);

        return this;
    }

    public GuardChain EnumDefined<T>(IEnumerable<T>? values, [CallerArgumentExpression("values")] string name = "")
        where T : struct, Enum
    {
        if (values == null)
            return this;

        var unknown = values.Where(x => !System.Enum.IsDefined(typeof(T), x)).ToArray();
        if (unknown.Any()) throw new ArgumentException($"Unknown enum values {string.Join(", ", unknown)}", name);

        return this;
    }

    public GuardChain EnumDefined<T>(T? value, [CallerArgumentExpression("value")] string name = "")
        where T : struct, Enum
    {
        if (value == null)
            return this;

        if (!System.Enum.IsDefined(typeof(T), value))
            throw new ArgumentException($"Unknown enum value {value}", name);

        return this;
    }

    public GuardChain NotEmpty(IEnumerable? values, [CallerArgumentExpression("values")] string name = "")
    {
        if (values == null)
            return this;
        
        IEnumerator? enumerator = null;

        try
        {
            enumerator = values.GetEnumerator();
            if (!enumerator.MoveNext()) throw new ArgumentException("Value might not be empty", name);
        }
        finally
        {
            if (enumerator is IDisposable disposable) disposable.Dispose();
        }

        return this;
    }

    public GuardChain Empty(IEnumerable? values, [CallerArgumentExpression("values")] string name = "")
    {
        if (values == null)
            return this;
        
        IEnumerator? enumerator = null;

        try
        {
            enumerator = values.GetEnumerator();
            if (enumerator.MoveNext()) throw new ArgumentException("Value might be empty", name);
        }
        finally
        {
            if (enumerator is IDisposable disposable) disposable.Dispose();
        }

        return this;
    }

    public GuardChain ASCII(string? value, [CallerArgumentExpression("value")] string name = "")
    {
        if (value == null)
            return this;
        
        if (!value.All(c => c <= 127))
            throw new ArgumentException("Might be an ASCII string", name);

        return this;
    }
}

internal static class Guard
{
    public static GuardChain NotNull<T>(T? value, [CallerArgumentExpression("value")] string name = "")
        where T : class
        => new GuardChain().NotNull(value, name);

    public static GuardChain NotNull<T>(T? value, [CallerArgumentExpression("value")] string name = "")
        where T : struct
        => new GuardChain().NotNull(value, name);

    public static GuardChain Null<T>(T? value, [CallerArgumentExpression("value")] string name = "")
        where T : class
        => new GuardChain().Null(value, name);

    public static GuardChain Null<T>(T? value, [CallerArgumentExpression("value")] string name = "")
        where T : struct
        => new GuardChain().Null(value, name);

    public static GuardChain Gte<T>(T? value, T than, [CallerArgumentExpression("value")] string name = "")
        where T : struct, IComparable<T>
        => new GuardChain().Gte(value, than, name);

    public static GuardChain Lt<T>(T? value, T than, [CallerArgumentExpression("value")] string name = "")
        where T : struct, IComparable<T>
        => new GuardChain().Lte(value, than, name);

    public static GuardChain Defined<T>(T? value, [CallerArgumentExpression("value")] string name = "")
        where T : struct, Enum
        => new GuardChain().EnumDefined(value, name);

    public static GuardChain Defined<T>(T value, [CallerArgumentExpression("value")] string name = "")
        where T : struct, Enum
        => new GuardChain().EnumDefined<T>(value, name);

    public static GuardChain Defined<T>(IEnumerable<T>? values, [CallerArgumentExpression("values")] string name = "")
        where T : struct, Enum
        => new GuardChain().EnumDefined(values, name);

    public static GuardChain NotEmpty(IEnumerable? values, [CallerArgumentExpression("values")] string name = "")
        => new GuardChain().NotEmpty(values, name);

    public static GuardChain Empty(IEnumerable? values, [CallerArgumentExpression("values")] string name = "")
        => new GuardChain().Empty(values, name);

    public static GuardChain ASCII(string? value, [CallerArgumentExpression("value")] string name = "")
        => new GuardChain().ASCII(value, name);

    public static Arg<T> Arg<T>(T value, [CallerArgumentExpression("value")] string name = "")
    {
        return new Arg<T>(name, value);
    }
}