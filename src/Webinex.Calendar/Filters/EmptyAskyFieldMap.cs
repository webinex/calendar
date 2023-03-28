using System.Linq.Expressions;
using Webinex.Asky;

namespace Webinex.Calendar.Filters;

internal class EmptyAskyFieldMap<TData> : IAskyFieldMap<TData>
    where TData : class, ICloneable
{
    public Expression<Func<TData, object>>? this[string fieldId] => null;
}