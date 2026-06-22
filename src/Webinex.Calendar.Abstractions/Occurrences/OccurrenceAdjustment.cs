namespace Webinex.Calendar;

/// <summary>
///     Represents occurrence adjustment whenever occurrence moved, cancelled or data updated
/// </summary>
/// <typeparam name="TData">Type of data</typeparam>
public class OccurrenceAdjustment<TData> : IEventEntityBase
    where TData : class, ICloneable
{
    /// <summary>
    ///     Occurrence identifier. Serialized value of <see cref="OccurrenceId"/>.
    ///     This value changes on ongoing move or update.
    /// </summary>
    public string Id { get; protected set; } = null!;

    public string RecurrentEventId { get; protected set; } = null!;

    /// <summary>
    ///     Period of original occurrence in <see cref="Event{TData}"/>.
    ///     This value changes on ongoing move of original event
    /// </summary>
    public Period<DateTimeOffset> Period { get; protected set; } = null!;

    /// <summary>
    ///     New position and duration of the occurrence
    /// </summary>
    public Period<DateTimeOffset>? MoveTo { get; protected set; }

    public EventGroupId Group { get; protected set; } = null!;
    public TData? Data { get; protected set; }
    public bool Cancelled { get; protected set; }

    public OccurrenceAdjustment(
        string id,
        string recurrentEventId,
        Period<DateTimeOffset> period,
        Period<DateTimeOffset>? moveTo,
        EventGroupId groupId,
        TData? data,
        bool cancelled)
    {
        groupId = groupId ?? throw new ArgumentNullException(nameof(groupId));
        period = period ?? throw new ArgumentNullException(nameof(period));

        if (cancelled && data != null)
            throw new ArgumentException("Cannot contain data when cancelled", nameof(data));

        if (cancelled && moveTo != null)
            throw new ArgumentException("Cannot be moved when cancelled", nameof(moveTo));

        Id = id ?? throw new ArgumentNullException(nameof(id));
        RecurrentEventId = recurrentEventId ?? throw new ArgumentNullException(nameof(recurrentEventId));
        Group = groupId.Clone();
        Data = (TData?)data?.Clone();
        Cancelled = cancelled;
        Period = period.Clone();
        MoveTo = moveTo?.Clone();
    }

    protected OccurrenceAdjustment()
    {
    }

    /// <summary>
    ///     Search window where adjustment may take effect.
    /// </summary>
    /// <returns>Adjustment effective period</returns>
    public Period<DateTimeOffset> Effective()
    {
        var start = MoveTo != null ? DateTimeOffsetUtil.Min(MoveTo.Start, Period.Start) : Period.Start;
        var end = MoveTo != null ? DateTimeOffsetUtil.Max(MoveTo.End, Period.End) : Period.End;
        return new Period<DateTimeOffset>(start, end);
    }

    public void Cancel()
    {
        Data = null;
        MoveTo = null;
        Cancelled = true;
    }

    public void SetData(TData? data)
    {
        if (Cancelled)
            throw new InvalidOperationException($"Unable to set data for cancelled occurrence {Id}");

        if (Data != data)
            Data = (TData?)data?.Clone();
    }

    public void Move(Period<DateTimeOffset> period)
    {
        if (Period == period)
        {
            MoveTo = null;
            return;
        }

        MoveTo = period;
    }

    public static OccurrenceAdjustment<TData> NewCancel(
        string recurrentEventId,
        EventGroupId groupId,
        Period<DateTimeOffset> period)
    {
        var id = new OccurrenceId(recurrentEventId, period.Start);
        return new OccurrenceAdjustment<TData>(
            id.ToString(),
            recurrentEventId,
            period,
            groupId: groupId,
            cancelled: true,
            data: null,
            moveTo: null);
    }

    public static OccurrenceAdjustment<TData> NewData(
        string recurrentEventId,
        EventGroupId groupId,
        Period<DateTimeOffset> period,
        TData data)
    {
        var id = new OccurrenceId(recurrentEventId, period.Start);
        return new OccurrenceAdjustment<TData>(
            id.ToString(),
            recurrentEventId,
            period,
            groupId: groupId,
            data: (TData)data.Clone(),
            moveTo: null,
            cancelled: false);
    }

    public static OccurrenceAdjustment<TData> NewUpdate(
        OccurrenceId id,
        Period<DateTimeOffset> period,
        EventGroupId groupId,
        TData? data,
        Period<DateTimeOffset>? moveTo)
    {
        return new OccurrenceAdjustment<TData>(
            id.ToString(),
            id.EventId,
            period,
            groupId: groupId,
            moveTo: moveTo,
            data: (TData?)data?.Clone(),
            cancelled: false);
    }

    public static OccurrenceAdjustment<TData> NewMove(
        string recurrentEventId,
        EventGroupId groupId,
        Period<DateTimeOffset> period,
        Period<DateTimeOffset> moveTo)
    {
        var id = new OccurrenceId(recurrentEventId, period.Start);
        return new OccurrenceAdjustment<TData>(
            id.ToString(),
            recurrentEventId,
            period,
            groupId: groupId,
            moveTo: moveTo,
            data: null,
            cancelled: false);
    }
}
