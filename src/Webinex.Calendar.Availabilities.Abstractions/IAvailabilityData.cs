namespace Webinex.Calendar.Availabilities;

public interface IAvailabilityData : ICloneable
{
    string TenantId { get; }
    string HostId { get; }
}