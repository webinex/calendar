namespace Webinex.Calendar.Common;

internal static class EnumerableExtensions
{
    public static IEnumerable<T> NotNull<T>(this IEnumerable<T?> enumerable)
        where T : class
    {
        return enumerable.Where(x => x != null).Cast<T>();
    }
}