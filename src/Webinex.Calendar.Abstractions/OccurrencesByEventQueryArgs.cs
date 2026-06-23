namespace Webinex.Calendar;

public class OccurrencesByEventQueryArgs
{
    public IReadOnlyCollection<string> Ids { get; }
    public OpenPeriod<DateTimeOffset>? Period { get; }
    public int? Count { get; }
    public bool IsRespectAdjustments { get; }
    public bool TryCache { get; }

    public OccurrencesByEventQueryArgs(
        IEnumerable<string> ids,
        int? count = null,
        OpenPeriod<DateTimeOffset>? period = null,
        bool isRespectAdjustments = false,
        bool tryCache = false)
    {
        Ids = ids?.Distinct().ToArray() ?? throw new ArgumentNullException(nameof(ids));
        Count = count;
        Period = period;
        IsRespectAdjustments = isRespectAdjustments;
        TryCache = tryCache;
    }
}