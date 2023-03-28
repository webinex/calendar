using Webinex.Calendar.Events;

namespace Webinex.Calendar;

public interface IOneTimeEventCalendarInstance<TData>
    where TData : class
{
    Task<OneTimeEvent<TData>?> GetAsync(Guid id);
    Task<OneTimeEvent<TData>[]> GetManyAsync(IEnumerable<Guid> ids);
    Task<OneTimeEvent<TData>> AddAsync(OneTimeEvent<TData> @event);
    Task<OneTimeEvent<TData>> UpdateDataAsync(OneTimeEvent<TData> @event, TData data);
    Task DeleteAsync(OneTimeEvent<TData> @event);
    Task CancelAsync(OneTimeEvent<TData> @event);
}