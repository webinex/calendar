using System.Linq.Expressions;
using Webinex.Asky;

namespace Webinex.Calendar.EntityFramework;

internal class EventRowAskyFieldMap<TData> : IAskyFieldMap<EventRow<TData>>
    where TData : class, ICloneable
{

    private readonly IAskyFieldMap<TData>? _dataFieldMap;

    public EventRowAskyFieldMap(IAskyFieldMap<TData>? dataFieldMap = null)
    {
        _dataFieldMap = dataFieldMap;
    }

    public Expression<Func<EventRow<TData>, object>>? this[string fieldId]
    {
        get
        {
            if (fieldId.StartsWith($"{Event.DATA_FIELD}."))
            {
                if (_dataFieldMap == null)
                    throw new InvalidOperationException(
                        $"No data field map registered in DI container for {typeof(TData).FullName}");
                
                return AskyFieldMap.Forward<EventRow<TData>, TData>(x => x.Data!, _dataFieldMap,
                    fieldId.Substring($"{Event.DATA_FIELD}.".Length))!;
            }

            return fieldId switch
            {
                "id" => x => x.Id,
                "type" => x => x.Type,
                "group.id" => x => x.Group.Id,
                "group.offset" => x => x.Group.Offset,
                "effective.start" => x => x.Effective.Start,
                "effective.end" => x => x.Effective.End,
                "recurrentEventId" => x => x.RecurrentEventId!,
                "period.start" => x => x.Period.Start,
                "period.end" => x => x.Period.End,
                "timeZone" => x => x.TimeZone!,
                "cancelled" => x => x.Cancelled!,
                "moveTo.start" => x => x.MoveTo != null ? x.MoveTo.Start : default(DateTimeOffset?)!,
                "moveTo.end" => x => x.MoveTo != null ? x.MoveTo.End : default(DateTimeOffset?)!,
                _ => null,
            };
        }
    }
}