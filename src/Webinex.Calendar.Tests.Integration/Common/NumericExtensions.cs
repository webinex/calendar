namespace Webinex.Calendar.Tests.Integration.Common;

internal static class NumericExtensions
{
    public static int Round(this double value) => (int)Math.Round(value, MidpointRounding.AwayFromZero);
}