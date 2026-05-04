namespace Webinex.Calendar;

[Flags]
public enum EventEntityType
{
    OneTimeEvent = 1,
    RecurrentEvent = 2,
    OccurrenceAdjustment = 4,
}