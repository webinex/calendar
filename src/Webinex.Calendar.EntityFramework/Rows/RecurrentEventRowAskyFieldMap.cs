using System.Linq.Expressions;
using Webinex.Asky;

namespace Webinex.Calendar.EntityFramework;

internal class RecurrentEventRowAskyFieldMap<TData> : IAskyFieldMap<RecurrentEventRow<TData>>
    where TData : class, ICloneable
{
    private readonly IAskyFieldMap<TData> _dataFieldMap;

    public RecurrentEventRowAskyFieldMap(IAskyFieldMap<TData> dataFieldMap)
    {
        _dataFieldMap = dataFieldMap;
    }

    public Expression<Func<RecurrentEventRow<TData>, object>>? this[string fieldId]
    {
        get
        {
            if (fieldId.StartsWith($"{Event.DATA_FIELD}."))
            {
                if (_dataFieldMap == null)
                    throw new InvalidOperationException(
                        $"No data field map registered in DI container for {typeof(TData).FullName}");

                return AskyFieldMap.Forward<RecurrentEventRow<TData>, TData>(x => x.Data!, _dataFieldMap,
                    fieldId.Substring($"{Event.DATA_FIELD}.".Length))!;
            }

            return fieldId switch
            {
                "id" => x => x.Id,
                "recurrentEventId" => x => x.Id,
                "period.start" => x => x.Period.Start,
                "period.end" => x => x.Period.End,
                "timeZone" => x => x.TimeZone!,
                "effective.start" => x => x.Effective.Start,
                "effective.end" => x => x.Effective.End!,
                "group.id" => x => x.Group.Id,
                "group.offset" => x => x.Group.Offset,
                _ => null,
            };
        }
    }
}