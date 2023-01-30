using Webinex.Asky;
using Webinex.Calendar.Common;
using Webinex.Calendar.Events;

namespace Webinex.Calendar;

public interface ICalendar<TData>
    where TData : class
{
    Task<OneTimeEvent<TData>?> GetOneTimeEventAsync(Guid id);
    Task<OneTimeEvent<TData>> AddOneTimeEventAsync(OneTimeEvent<TData> @event);
    Task<OneTimeEvent<TData>> UpdateDataAsync(OneTimeEvent<TData> @event, TData data);
    Task DeleteAsync(OneTimeEvent<TData> @event);

    Task<RecurrentEvent<TData>?> GetRecurrentAsync(Guid id);
    Task<Event<TData>> AddOrUpdateRecurrentDataAsync(RecurrentEvent<TData> @event, DateTimeOffset eventStart, TData data);
    Task<Event<TData>> AddRecurrentStateAsync(RecurrentEvent<TData> @event, DateTimeOffset start, TData data);
    Task<Event<TData>> UpdateRecurrentDataAsync(RecurrentEvent<TData> @event, DateTimeOffset date, TData data);
    Task<RecurrentEvent<TData>> AddRecurrentEventAsync(RecurrentEvent<TData> @event);
    Task MoveRecurrentEventAsync(RecurrentEvent<TData> @event, DateTimeOffset eventStart, Period moveTo);
    Task CancelRecurrentEventSinceAsync(Guid id, DateTimeOffset since);
    Task CancelOneRecurrentEventAsync(RecurrentEvent<TData> @event, DateTimeOffset eventStart);

    Task<Event<TData>[]> GetAllAsync(DateTimeOffset from, DateTimeOffset to, FilterRule? dataFilterRule = null);
}