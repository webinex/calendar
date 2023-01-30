using System.Linq.Expressions;
using Webinex.Asky;

namespace Webinex.Calendar.Tests.Integration.Setups;

public class EventDataAskyFieldMap : IAskyFieldMap<EventData>
{
    public Expression<Func<EventData, object>>? this[string fieldId] =>
        fieldId switch
        {
            "name" => x => x.Name,
            _ => null,
        };
}