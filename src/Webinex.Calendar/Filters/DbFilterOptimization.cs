namespace Webinex.Calendar.Filters;

[Flags]
public enum DbFilterOptimization
{
    None = 0,

    /// <summary>
    /// Disable using of OneTime events
    /// </summary>
    OneTime = 1,

    /// <summary>
    /// Disable using of DayOfMonth events
    /// </summary>
    DayOfMonth = 1 << 1,

    /// <summary>
    /// Disable using of DayOfWeek events
    /// </summary>
    DayOfWeek = 1 << 2,

    /// <summary>
    /// Disable using of Interval events
    /// </summary>
    Interval = 1 << 3,
    
    /// <summary>
    /// Disable using of events state. It's useful when you don't need functionality for moving events and updating data 
    /// </summary>
    State = 1 << 4,

    /// <summary>
    /// If enabled, filter event rows by data in DB otherwise on the client
    /// </summary>
    Data = 1 << 5,

    /// <summary>
    /// If enabled, filter event rows in DB only, otherwise filter on the client 
    /// </summary>
    Precise = 1 << 6,

    Default = OneTime | DayOfMonth | DayOfWeek | Interval | State | Data | Precise
}