namespace Webinex.Calendar.Common;

internal static class EnumerableUtil
{
    /// <summary>
    ///     Lazily merges sources of ordered items into a single ordered stream.
    ///     Each source must be ordered by <paramref name="keySelector"/>; source order itself does not matter.
    /// </summary>
    public static IEnumerable<T> MergeOrdered<T, TKey>(
        IEnumerable<IEnumerable<T>> sources,
        Func<T, TKey> keySelector)
        where TKey : notnull, IComparable<TKey>
    {
        var queue = new PriorityQueue<IEnumerator<T>, TKey>();

        try
        {
            foreach (var source in sources)
            {
                var enumerator = source.GetEnumerator();

                // Prime every source with its first item. The priority queue then always knows
                // the next candidate from each source without materializing the rest.
                if (enumerator.MoveNext())
                {
                    queue.Enqueue(enumerator, keySelector(enumerator.Current));
                }
                else
                {
                    enumerator.Dispose();
                }
            }

            while (queue.TryDequeue(out var enumerator, out _))
            {
                yield return enumerator.Current;

                // Advance only the source that produced the yielded item. This keeps the merge lazy,
                // so callers can Take(count) from endless sources without enumerating beyond count.
                if (enumerator.MoveNext())
                {
                    queue.Enqueue(enumerator, keySelector(enumerator.Current));
                }
                else
                {
                    enumerator.Dispose();
                }
            }
        }
        finally
        {
            while (queue.TryDequeue(out var enumerator, out _))
            {
                enumerator.Dispose();
            }
        }
    }
}
