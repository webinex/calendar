using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
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
            typeof(IAskyFieldMap<>).MakeGenericType(typeof(RecurrentEventRow<>).MakeGenericType(configuration.DataType)),
            typeof(RecurrentEventRowAskyFieldMap<>).MakeGenericType(configuration.DataType));
        
        configuration.Services.AddScoped(
            typeof(IEventRepository<>).MakeGenericType(configuration.DataType),
            typeof(EventRepository<>).MakeGenericType(configuration.DataType));

        return configuration;
    }
}