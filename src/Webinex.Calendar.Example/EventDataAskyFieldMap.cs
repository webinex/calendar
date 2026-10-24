using System.Linq.Expressions;
using Webinex.Asky;

namespace Webinex.Calendar.Example;

public class EventDataAskyFieldMap : IAskyFieldMap<EventData>
{
    public Expression<Func<EventData, object>>? this[string fieldId] =>
        fieldId == "title" ? x => x.Title : null;
}
