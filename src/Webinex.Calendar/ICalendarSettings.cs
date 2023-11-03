namespace Webinex.Calendar;

public interface ICalendarSettings
{
    string TimeZone { get; }
}

public interface ICalendarSettings<TData> : ICalendarSettings
{
}