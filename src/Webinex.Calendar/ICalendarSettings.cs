namespace Webinex.Calendar;

public interface ICalendarSettings
{
    TimeZoneInfo TimeZone { get; }
}

public interface ICalendarSettings<TData> : ICalendarSettings
{
}