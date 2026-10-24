using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Webinex.Asky;

namespace Webinex.Calendar.EntityFramework;

public static class CalendarConfigurationExtensions
{
    public static ICalendarConfiguration UseDbContext<TDbContext>(this ICalendarConfiguration configuration)
        where TDbContext : DbContext
    {
        configuration.Data["DbContextType"] = typeof(TDbContext);

        configuration.Services.AddScoped(
            typeof(ICalendarDbContextProvider<>).MakeGenericType(configuration.DataType),
            typeof(CalendarDbContextProvider<,>).MakeGenericType(configuration.DataType, typeof(TDbContext)));

        configuration.Services.AddTransient(
            typeof(IAskyFieldMap<>).MakeGenericType(typeof(EventRow<>).MakeGenericType(configuration.DataType)),
            typeof(EventRowAskyFieldMap<>).MakeGenericType(configuration.DataType));

        configuration.Services.AddTransient(
            typeof(IAskyFieldMap<>).MakeGenericType(
                typeof(RecurrentEventRow<>).MakeGenericType(configuration.DataType)),
            typeof(RecurrentEventRowAskyFieldMap<>).MakeGenericType(configuration.DataType));

        configuration.Services.AddScoped(
            typeof(IEventRepository<>).MakeGenericType(configuration.DataType),
            typeof(EventRepository<>).MakeGenericType(configuration.DataType));

        return configuration;
    }

    public static ICalendarConfiguration UseQueryOptimization(this ICalendarConfiguration configuration, bool enabled)
    {
        return configuration.UseQueryOptimization(enabled
            ? new EventQueryOptimization()
            : EventQueryOptimization.Disable);
    }

    public static ICalendarConfiguration UseQueryOptimization(this ICalendarConfiguration configuration, EventQueryOptimization value)
    {
        var type = typeof(EventQueryOptimization<>).MakeGenericType(configuration.DataType);
        configuration.Services.RemoveAll(type);
        var typed = NewTypedOptimization(configuration.DataType, value);
        configuration.Services.AddSingleton(type, typed);
        return configuration;
    }

    private static EventQueryOptimization NewTypedOptimization(Type dataType, EventQueryOptimization value)
    {
        var type = typeof(EventQueryOptimization<>).MakeGenericType(dataType);
        var newMethod = type.GetMethod(nameof(EventQueryOptimization<>.New), BindingFlags.Static | BindingFlags.Public)!;
        return (EventQueryOptimization)newMethod.Invoke(null, [value])!;
    }
}