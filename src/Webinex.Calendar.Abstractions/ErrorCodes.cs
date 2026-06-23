// ReSharper disable once CheckNamespace

namespace Webinex.Coded;

public static class CalendarCodes
{
    public static readonly Code NO_OCCURRENCE = Code.INVALID.Child("CALENDAR.NO_OCCURRENCE");
    
    public static CodedFailure<string> NoOccurrence(this ThisCodedFailure _, string id)
    {
        return new CodedFailure<string>(NO_OCCURRENCE, id);
    }
}