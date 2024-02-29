using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Webinex.Asky;
using Webinex.Calendar.Caches;
using Webinex.Calendar.DataAccess;
using Webinex.Calendar.Filters;

namespace Webinex.Calendar;

public interface ICalendarConfiguration
{
    Type EventDataType { get; }
    Type EventRowType { get; }
    IDictionary<string, object> Values { get; }

    ICalendarConfiguration UseTimeZone(string timeZone);
    ICalendarConfiguration UseDbFilterOptimization(DbFilterOptimization optimization);
    ICalendarConfiguration AddDbContext<TDbContext>() where TDbContext : DbContext;
    ICalendarConfiguration AddAskyFieldMap<T>();
    ICalendarConfiguration AddCache(TimeSpan lt, TimeSpan gte, TimeSpan tick);
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

        Settings = (CalendarSettings)Activator.CreateInstance(
            typeof(CalendarSettings<>).MakeGenericType(EventDataType))!;

        _services.AddSingleton(typeof(ICalendarSettings<>).MakeGenericType(EventDataType), Settings);
    }

    private Type CalendarDbContextType => typeof(ICalendarDbContext<>).MakeGenericType(EventDataType);
    private Type AskyFieldMapInterfaceType => typeof(IAskyFieldMap<>).MakeGenericType(EventDataType);

    public Type EventDataType { get; }
    public Type EventRowType => typeof(EventRow<>).MakeGenericType(EventDataType);

    public IDictionary<string, object> Values { get; } = new Dictionary<string, object>();
    private CalendarSettings Settings { get; }

    public ICalendarConfiguration UseTimeZone(string timeZone)
    {
        Settings.TimeZone = timeZone;
        return this;
    }

    public ICalendarConfiguration UseDbFilterOptimization(DbFilterOptimization optimization)
    {
        Settings.DbFilterOptimization = optimization;
        return this;
    }

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

    public ICalendarConfiguration AddCache(TimeSpan lt, TimeSpan gte, TimeSpan tick)
    {
        var cacheStoreType = typeof(CacheStore<>).MakeGenericType(EventDataType);

        _services.AddSingleton(cacheStoreType);

        _services.AddSingleton(
            typeof(ICacheStore<>).MakeGenericType(EventDataType),
            x => x.GetRequiredService(cacheStoreType));

        _services.AddSingleton(
            typeof(IHostedService),
            x => x.GetRequiredService(cacheStoreType));

        _services.AddScoped(
            typeof(ICache<>).MakeGenericType(EventDataType),
            typeof(Cache<>).MakeGenericType(EventDataType));

        _services.AddSingleton(
            typeof(CalendarCacheOptions<>).MakeGenericType(EventDataType),
            CalendarCacheOptions.NewEnabled(EventDataType, lt, gte, tick));

        return this;
    }

    public void Complete()
    {
        AddEmptyAskyFieldMapIfNotConfigured();
        AddNoCacheIfNotConfigured();
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

    private void AddNoCacheIfNotConfigured()
    {
        var service = _services.Any(x => x.ServiceType == typeof(ICache<>).MakeGenericType(EventDataType));

        if (!service)
        {
            _services.AddSingleton(
                typeof(ICache<>).MakeGenericType(EventDataType),
                typeof(NoCache<>).MakeGenericType(EventDataType));
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

    private class CalendarSettings : ICalendarSettings
    {
        public string TimeZone { get; set; } = TimeZoneInfo.Utc.Id;
        public DbFilterOptimization DbFilterOptimization { get; set; } = DbFilterOptimization.Default;
    }

    private class CalendarSettings<TData> : CalendarSettings, ICalendarSettings<TData>
    {
    }
}