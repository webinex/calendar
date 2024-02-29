using Webinex.Calendar.Filters;

namespace Webinex.Calendar;

public interface ICalendarSettings
{
    string TimeZone { get; }
    DbFilterOptimization DbQueryOptimization { get; }
}

public interface ICalendarSettings<TData> : ICalendarSettings
{
}