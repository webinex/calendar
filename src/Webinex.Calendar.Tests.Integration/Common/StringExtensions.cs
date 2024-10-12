namespace Webinex.Calendar.Tests.Integration.Common;

public static class StringExtensions
{
    /// <summary>
    /// Converts string in format "HH:mm:ss" or "HH:mm" to <see cref="TimeOnly"/>
    /// </summary>
    public static TimeOnly ToTimeOnly(this string str) => TimeOnly.Parse(str);
}