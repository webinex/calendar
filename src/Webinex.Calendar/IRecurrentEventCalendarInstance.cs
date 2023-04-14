using Webinex.Asky;
using Webinex.Calendar.Common;
using Webinex.Calendar.Events;

namespace Webinex.Calendar;

public interface IRecurrentEventCalendarInstance<TData>
    where TData : class
{
    Task<RecurrentEvent<TData>?> GetAsync(Guid id);
    Task<RecurrentEvent<TData>[]> GetManyAsync(IEnumerable<Guid> ids);
    Task<RecurrentEvent<TData>[]> GetManyAsync(FilterRule filter, SortRule? sortRule = null, PagingRule? pagingRule = null);
    Task<RecurrentEvent<TData>> AddAsync(RecurrentEvent<TData> @event);
    Task<RecurrentEvent<TData>[]> AddRangeAsync(IEnumerable<RecurrentEvent<TData>> events);
    Task MoveAsync(RecurrentEvent<TData> @event, DateTimeOffset eventStart, Period moveTo);
    Task CancelAsync(Guid id, DateTimeOffset since);
    Task CancelAppearanceAsync(RecurrentEvent<TData> @event, DateTimeOffset eventStart);
    Task DeleteAsync(Guid id);
    Task DeleteAsync(RecurrentEvent<TData> @event);
    Task DeleteRangeAsync(IEnumerable<RecurrentEvent<TData>> events);

    Task<RecurrentEventState<TData>?> GetStateAsync(RecurrentEventStateId id);
    Task<RecurrentEventState<TData>[]> GetManyStatesAsync(IEnumerable<RecurrentEventStateId> ids);
    Task<Event<TData>> SaveDataAsync(RecurrentEvent<TData> @event, DateTimeOffset eventStart, TData data);
    Task<Event<TData>> AddDataAsync(RecurrentEvent<TData> @event, DateTimeOffset start, TData data);
    Task<Event<TData>> UpdateDataAsync(RecurrentEvent<TData> @event, DateTimeOffset date, TData data);
    Task DeleteStateAsync(RecurrentEventStateId id);
}

public static class RecurrentEventCalendarInstanceExtensions
{
    public static Task<RecurrentEventState<TData>?> GetStateAsync<TData>(
        this IRecurrentEventCalendarInstance<TData> calendar,
        Guid recurrentEventId,
        DateTimeOffset eventStart)
        where TData : class
    {
        calendar = calendar ?? throw new ArgumentNullException(nameof(calendar));
        return calendar.GetStateAsync(new RecurrentEventStateId(recurrentEventId, eventStart));
    }
}