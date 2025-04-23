using System.Linq.Expressions;
using Webinex.Asky;

namespace Webinex.Calendar;

internal class OccurrenceFieldMap<TData> : IAskyFieldMap<Occurrence<TData>>
    where TData : class, ICloneable
{
    private static readonly string DATA_PREFIX = Event.DATA_FIELD + ".";
    private readonly IAskyFieldMap<TData>? _dataAskyFieldMap;

    public OccurrenceFieldMap(IAskyFieldMap<TData>? dataAskyFieldMap = null)
    {
        _dataAskyFieldMap = dataAskyFieldMap;
    }

    public Expression<Func<Occurrence<TData>, object>>? this[string fieldId]
    {
        get
        {
            if (fieldId.StartsWith(DATA_PREFIX))
            {
                if (_dataAskyFieldMap == null)
                    throw new InvalidOperationException(
                        $"No data field map registered in DI container for {typeof(TData).FullName}");
                
                return AskyFieldMap.Forward<Occurrence<TData>, TData>(x => x.Data, _dataAskyFieldMap,
                    fieldId.Substring(DATA_PREFIX.Length))!;
            }

            return fieldId switch
            {
                "id" => x => x.Id,
                "period.start" => x => x.Period.Start,
                "period.end" => x => x.Period.End,
                "group.id" => x => x.Group.Id,
                "group.offset" => x => x.Group.Offset,
                _ => throw new ArgumentOutOfRangeException(nameof(fieldId), fieldId, null)
            };
        }
    }
}