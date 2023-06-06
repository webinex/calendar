using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Webinex.Asky;
using Webinex.Calendar.DataAccess;
using Webinex.Calendar.Filters;

namespace Webinex.Calendar;

public interface ICalendarConfiguration
{
    Type EventDataType { get; }
    Type EventRowType { get; }
    IDictionary<string, object> Values { get; }

    ICalendarConfiguration AddDbContext<TDbContext>() where TDbContext : DbContext;
    ICalendarConfiguration AddAskyFieldMap<T>();
}

internal class CalendarConfiguration : ICalendarConfiguration
{
    private readonly IServiceCollection _services;

    internal CalendarConfiguration(Type eventDataType, IServiceCollection services)
    {
        _services = services ?? throw new ArgumentNullException(nameof(services));
        EventDataType = eventDataType ?? throw new ArgumentNullException(nameof(eventDataType));

        _services.AddScoped(
            typeof(ICalendar<>).MakeGenericType(EventDataType),
            typeof(Calendar<>).MakeGenericType(EventDataType));

        _services.AddScoped(
            typeof(IRecurrentEventRowAskyFieldMap<>).MakeGenericType(EventDataType),
            typeof(RecurrentEventRowAskyFieldMap<>).MakeGenericType(EventDataType));
        
        _services.AddScoped(
            typeof(IRecurrentEventStateAskyFieldMap<>).MakeGenericType(EventDataType),
            typeof(RecurrentEventStateAskyFieldMap<>).MakeGenericType(EventDataType));
    }

    private Type CalendarDbContextType => typeof(ICalendarDbContext<>).MakeGenericType(EventDataType);
    private Type AskyFieldMapInterfaceType => typeof(IAskyFieldMap<>).MakeGenericType(EventDataType);

    public Type EventDataType { get; }
    public Type EventRowType => typeof(EventRow<>).MakeGenericType(EventDataType);
    public IDictionary<string, object> Values { get; } = new Dictionary<string, object>();

    public ICalendarConfiguration AddDbContext<TDbContext>()
        where TDbContext : DbContext
    {
        AssertCorrectDbContextType(typeof(TDbContext));
        _services.AddScoped(CalendarDbContextType, sp => sp.GetService(typeof(TDbContext))!);
        return this;
    }

    public ICalendarConfiguration AddAskyFieldMap<T>()
    {
        if (!typeof(T).IsAssignableTo(AskyFieldMapInterfaceType))
        {
            throw new InvalidOperationException(
                $"{typeof(T).FullName} might be assignable to {AskyFieldMapInterfaceType.FullName}");
        }

        _services.AddScoped(AskyFieldMapInterfaceType, typeof(T));
        return this;
    }

    public void Complete()
    {
        AddEmptyAskyFieldMapIfNotConfigured();
    }

    private void AddEmptyAskyFieldMapIfNotConfigured()
    {
        var askyFieldMapConfigured = _services.Any(x => x.ServiceType == AskyFieldMapInterfaceType);

        if (!askyFieldMapConfigured)
        {
            _services.AddSingleton(AskyFieldMapInterfaceType,
                typeof(EmptyAskyFieldMap<>).MakeGenericType(EventDataType));
        }
    }

    private void AssertCorrectDbContextType(Type type)
    {
        if (!type.IsAssignableTo(CalendarDbContextType))
        {
            throw new InvalidOperationException(
                $"{type.FullName} might be assignable to {CalendarDbContextType.FullName}");
        }
    }
}