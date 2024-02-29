using Webinex.Calendar.Filters;

namespace Webinex.Calendar;

public interface ICalendarSettings
{
    string TimeZone { get; }
    DbFilterOptimization DbFilterOptimization { get; }
}

public interface ICalendarSettings<TData> : ICalendarSettings
{
}