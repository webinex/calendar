namespace Webinex.Calendar.Availabilities;

internal class Availability<TData> : IAvailability<TData>
    where TData : class, IAvailabilityData
{
    private readonly AvailabilityOccurrenceSaveService<TData> _availabilityOccurrenceSaveService;
    private readonly AvailabilityGroupSaveService<TData> _availabilityGroupSaveService;

    public Availability(
        AvailabilityOccurrenceSaveService<TData> availabilityOccurrenceSaveService,
        AvailabilityGroupSaveService<TData> availabilityGroupSaveService)
    {
        _availabilityOccurrenceSaveService = availabilityOccurrenceSaveService;
        _availabilityGroupSaveService = availabilityGroupSaveService;
    }

    public Task SaveAsync(SaveAvailabilityArgs<TData> args)
    {
        return args.Behavior switch
        {
            OccurenceUpdateBehavior.Occurrence => _availabilityOccurrenceSaveService.SaveAsync(args),
            OccurenceUpdateBehavior.Group => _availabilityGroupSaveService.SaveAsync(args),
            _ => throw new ArgumentOutOfRangeException(nameof(args.Behavior), args.Behavior, null)
        };
    }
}