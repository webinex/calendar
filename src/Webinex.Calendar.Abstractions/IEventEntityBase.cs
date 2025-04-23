namespace Webinex.Calendar;

public interface IEventEntityBase
{
    string Id { get; }
    Period<DateTimeOffset> Period { get; }
}