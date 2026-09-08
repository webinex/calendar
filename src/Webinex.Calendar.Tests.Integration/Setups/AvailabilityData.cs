using Webinex.Calendar.Availabilities;

namespace Webinex.Calendar.Tests.Integration.Setups;

public class AvailabilityData : IAvailabilityData
{
    protected AvailabilityData()
    {
    }

    public AvailabilityData(string tenantId, string hostId)
    {
        TenantId = tenantId;
        HostId = hostId;
    }

    public string TenantId { get; protected set; } = null!;
    public string HostId { get; protected set; } = null!;

    public object Clone()
    {
        return new AvailabilityData(TenantId, HostId);
    }
}
