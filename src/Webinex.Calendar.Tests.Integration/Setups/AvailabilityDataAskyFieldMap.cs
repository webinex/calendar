using System.Linq.Expressions;
using Webinex.Asky;

namespace Webinex.Calendar.Tests.Integration.Setups;

public class AvailabilityDataAskyFieldMap : IAskyFieldMap<AvailabilityData>
{
    public Expression<Func<AvailabilityData, object>>? this[string fieldId] =>
        fieldId switch
        {
            "tenantId" => x => x.TenantId,
            "hostId" => x => x.HostId,
            _ => null,
        };
}
