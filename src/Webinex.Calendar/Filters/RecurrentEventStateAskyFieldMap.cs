using System.Linq.Expressions;
using Webinex.Asky;
using Webinex.Calendar.DataAccess;

namespace Webinex.Calendar.Filters;

internal interface IRecurrentEventStateAskyFieldMap<TData> : IAskyFieldMap<EventRow<TData>>
    where TData : class, ICloneable
{
}

internal class RecurrentEventStateAskyFieldMap<TData> : IRecurrentEventStateAskyFieldMap<TData>
    where TData : class, ICloneable
{
    private const string DATA_PREFIX = "data.";
    
    private readonly IAskyFieldMap<TData> _dataFieldMap;

    public RecurrentEventStateAskyFieldMap(IAskyFieldMap<TData> dataFieldMap)
    {
        _dataFieldMap = dataFieldMap;
    }
    
    public Expression<Func<EventRow<TData>, object>>? this[string fieldId]
    {
        get
        {
            if (fieldId.StartsWith(DATA_PREFIX))
            {
                return AskyFieldMap.Forward<EventRow<TData>, TData>(x => x.Data, _dataFieldMap,
                    fieldId.Substring(DATA_PREFIX.Length));
            }
            
            return fieldId switch
            {
                "recurrentEventId" => x => x.RecurrentEventId!,
                "period.start" => x => x.Effective.Start,
                "period.end" => x => x.Effective.End!,
                "moveTo.start" => x => (x.MoveTo != null ? x.MoveTo.Start : default(DateTimeOffset?))!,
                "moveTo.end" => x => (x.MoveTo != null ? x.MoveTo.End : default(DateTimeOffset?))!,
                _ => null,
            };
        }
    }
}