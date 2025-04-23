namespace Webinex.Calendar.Availabilities;

public interface IAvailability<TData>
    where TData : class, IAvailabilityData
{
    Task SaveAsync(SaveAvailabilityArgs<TData> args);
}