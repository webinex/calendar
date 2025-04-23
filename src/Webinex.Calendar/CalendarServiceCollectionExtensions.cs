using Microsoft.Extensions.DependencyInjection;

namespace Webinex.Calendar;

public static class CalendarServiceCollectionExtensions
{
    public static IServiceCollection AddCalendar<TData>(
        this IServiceCollection services,
        Action<ICalendarConfiguration> configure)
        where TData : class, ICloneable
    {
        services = services ?? throw new ArgumentNullException(nameof(services));
        configure = configure ?? throw new ArgumentNullException(nameof(configure));

        var configuration = CalendarConfiguration.GetOrCreate<TData>(services);
        configure(configuration);

        return services;
    }
}