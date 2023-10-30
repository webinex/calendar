namespace Webinex.Calendar.Repeats;

internal static class RepeatMatchBaseExtensions
{
    private const int DAY_MINUTES = 24 * 60;

    private static int TotalEndMinutes(this IRepeatBase match)
    {
        return match.TimeOfTheDayInMinutes + match.DurationMinutes;
    }

    private static bool IsOvernight(this IRepeatBase match)
    {
        return match.TotalEndMinutes() > DAY_MINUTES;
    }

    public static int? OvernightMinutes(this IRepeatBase match)
    {
        return match.IsOvernight() ? match.TotalEndMinutes() - DAY_MINUTES : null;
    }

    public static int SameDayLastTime(this IRepeatBase match)
    {
        return match.IsOvernight() ? DAY_MINUTES : match.TotalEndMinutes();
    }
}