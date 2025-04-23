using Microsoft.Extensions.DependencyInjection;

namespace Webinex.Calendar.Availabilities;

public static class AvailabilityServiceCollectionExtensions
{
    public static IServiceCollection AddAvailability<TData>(
        this IServiceCollection services,
        Action<ICalendarConfiguration> configure)
        where TData : class, IAvailabilityData
    {
        return services.AddCalendar<TData>(x =>
        {
            x.Services.AddScoped<IAvailability<TData>, Availability<TData>>();
            x.Services.AddScoped<AvailabilityOccurrenceSaveService<TData>>();
            x.Services.AddScoped<AvailabilityGroupSaveService<TData>>();
            configure(x);
        });
    }
}