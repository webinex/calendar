using System.Linq.Expressions;
using Webinex.Asky;
using Webinex.Calendar.DataAccess;

namespace Webinex.Calendar.Filters;

internal interface IRecurrentEventRowAskyFieldMap<TData> : IAskyFieldMap<EventRow<TData>>
    where TData : class, ICloneable
{
}

internal class RecurrentEventRowAskyFieldMap<TData> : IRecurrentEventRowAskyFieldMap<TData>
    where TData : class, ICloneable
{
    private const string DATA_PREFIX = "data.";

    private readonly IAskyFieldMap<TData> _dataFieldMap;

    public RecurrentEventRowAskyFieldMap(IAskyFieldMap<TData> dataFieldMap)
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
                    fieldId.Substring(DATA_PREFIX.Length))!;
            }

            return fieldId switch
            {
                "id" => x => x.Id,
                "recurrentEventId" => x => x.RecurrentEventId!,
                "effective.start" => x => x.Effective.Start,
                "effective.end" => x => x.Effective.End!,
                "repeat.weekday.monday" => x => (x.Repeat != null ? x.Repeat.Monday : default(bool?))!,
                "repeat.weekday.tuesday" => x => (x.Repeat != null ? x.Repeat.Tuesday : default(bool?))!,
                "repeat.weekday.wednesday" => x => (x.Repeat != null ? x.Repeat.Wednesday : default(bool?))!,
                "repeat.weekday.thursday" => x => (x.Repeat != null ? x.Repeat.Thursday : default(bool?))!,
                "repeat.weekday.friday" => x => (x.Repeat != null ? x.Repeat.Friday : default(bool?))!,
                "repeat.weekday.saturday" => x => (x.Repeat != null ? x.Repeat.Saturday : default(bool?))!,
                "repeat.weekday.sunday" => x => (x.Repeat != null ? x.Repeat.Sunday : default(bool?))!,
                _ => null,
            };
        }
    }
}