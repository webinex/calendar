using Microsoft.Extensions.DependencyInjection;

namespace Webinex.Calendar;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCalendar<TData>(this IServiceCollection services)
        where TData : class, ICloneable
    {
        services.AddScoped<ICalendar<TData>, Calendar<TData>>();
        return services;
    }
}