namespace Webinex.Calendar.Availabilities;

public class SaveAvailabilityArgs<TData>
    where TData : class, IAvailabilityData
{
    public string TenantId { get; protected set; }
    public string HostId { get; protected set; }
    public DateOnly StartOfWeek { get; protected set; }
    public string TimeZone { get; protected set; }
    public IReadOnlyCollection<Item> Available { get; protected set; }
    public OccurenceUpdateBehavior Behavior { get; protected set; }

    public record Item(Period<DateTimeOffset> Period, TData Data);

    public SaveAvailabilityArgs(
        string tenantId,
        string hostId,
        DateOnly startOfWeek,
        string timeZone,
        IEnumerable<Item> available,
        OccurenceUpdateBehavior behavior = OccurenceUpdateBehavior.Occurrence)
    {
        TenantId = tenantId;
        HostId = hostId;
        StartOfWeek = startOfWeek;
        TimeZone = timeZone;
        Available = available.ToArray();
        Behavior = behavior;
    }
}