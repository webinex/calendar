using Webinex.Asky;
using Webinex.Calendar.Events;

namespace Webinex.Calendar;

public interface ICalendar<TData>
    where TData : class
{
    IOneTimeEventCalendarInstance<TData> OneTime { get; }
    IRecurrentEventCalendarInstance<TData> Recurrent { get; }

    Task<Event<TData>[]> GetCalculatedAsync(DateTimeOffset from, DateTimeOffset to, FilterRule? dataFilterRule = null, QueryOptions queryOptions = QueryOptions.Db);
}