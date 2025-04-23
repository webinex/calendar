using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Webinex.Asky;
using Webinex.Calendar.Caches;
using Webinex.Calendar.Services;

namespace Webinex.Calendar;

public interface ICalendarConfiguration
{
    Type DataType { get; }
    IDictionary<string, object> Data { get; }
    IServiceCollection Services { get; }
    // TODO: restore cache services
    // ICalendarConfiguration AddCache(TimeSpan lt, TimeSpan gte, TimeSpan tick);
}

internal class CalendarConfiguration : ICalendarConfiguration
{
    public Type DataType { get; }
    public IDictionary<string, object> Data { get; } = new Dictionary<string, object>();
    public IServiceCollection Services { get; }

    private CalendarConfiguration(Type eventDataType, IServiceCollection services)
    {
        Services = services ?? throw new ArgumentNullException(nameof(services));
        DataType = eventDataType ?? throw new ArgumentNullException(nameof(eventDataType));

        Services.AddSingleton(this);

        Services.AddScoped(
            typeof(ICalendar<>).MakeGenericType(DataType),
            typeof(Calendar<>).MakeGenericType(DataType));

        Services.AddTransient(
            typeof(IAskyFieldMap<>).MakeGenericType(typeof(Occurrence<>).MakeGenericType(DataType)),
            typeof(OccurrenceFieldMap<>).MakeGenericType(DataType));

        Services.AddTransient(
            typeof(IOccurrenceUpdateService<>).MakeGenericType(DataType),
            typeof(OccurrenceUpdateService<>).MakeGenericType(DataType));
        
        Services.AddTransient(typeof(RecurrentEventUpdateService<>).MakeGenericType(DataType));

        Services.AddTransient(
            typeof(IOccurrenceCancellationService<>).MakeGenericType(DataType),
            typeof(OccurrenceCancellationService<>).MakeGenericType(DataType));

        Services.AddTransient(
            typeof(IOccurrenceReadService<>).MakeGenericType(DataType),
            typeof(OccurrenceReadService<>).MakeGenericType(DataType));
    }

    public ICalendarConfiguration AddCache(TimeSpan lt, TimeSpan gte, TimeSpan tick)
    {
        var cacheStoreType = typeof(CacheStore<>).MakeGenericType(DataType);

        Services.AddSingleton(cacheStoreType);

        Services.AddSingleton(
            typeof(ICacheStore<>).MakeGenericType(DataType),
            x => x.GetRequiredService(cacheStoreType));

        Services.AddSingleton(
            typeof(IHostedService),
            x => x.GetRequiredService(cacheStoreType));

        Services.AddScoped(
            typeof(ICache<>).MakeGenericType(DataType),
            typeof(Cache<>).MakeGenericType(DataType));

        Services.AddSingleton(
            typeof(CalendarCacheOptions<>).MakeGenericType(DataType),
            CalendarCacheOptions.NewEnabled(DataType, lt, gte, tick));

        return this;
    }

    internal static CalendarConfiguration GetOrCreate<TData>(IServiceCollection services)
    {
        var instance = (CalendarConfiguration?) services.FirstOrDefault(x =>
            x.ServiceType == typeof(CalendarConfiguration) &&
            ((CalendarConfiguration)x.ImplementationInstance!).DataType == typeof(TData))?.ImplementationInstance;

        return instance ?? new CalendarConfiguration(typeof(TData), services);
    }
}