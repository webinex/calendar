namespace Webinex.Calendar;

public class UpdateOccurrenceArgs<TData>
{
    public OccurrenceId Id { get; protected set; }
    public Optional<Period<DateTimeOffset>>? Period { get; protected set; }
    public Optional<string>? TimeZone { get; protected set; }
    public Optional<TData?>? Data { get; protected set; }

    /// <summary>
    ///     Recurrence rules update argument, can only be specified with <see cref="Behavior"/> <see cref="OccurenceUpdateBehavior.Group"/>
    /// </summary>
    public Optional<Recurrence>? Recurrence { get; protected set; }

    public OccurenceUpdateBehavior Behavior { get; protected set; }

    /// <summary>
    ///     Creates new instance of <see cref="UpdateOccurrenceArgs{TData}"/>
    /// </summary>
    /// <param name="id">
    ///     Occurrence id. String representation of <see cref="OccurrenceId"/>
    /// </param>
    /// <param name="period">
    ///     New occurrence period
    /// </param>
    /// <param name="timeZone">
    ///     New time zone
    /// </param>
    /// <param name="data">
    ///     New occurrence data. When <paramref name="behavior"/> = <see cref="OccurenceUpdateBehavior.Occurrence"/>, updates data
    ///     only for specific occurrence. When <paramref name="behavior"/> = <see cref="OccurenceUpdateBehavior.Group"/> creates new
    ///     recurrent event with new data.
    /// </param>
    /// <param name="recurrence">
    ///     Recurrence rules update argument, can only be specified with <paramref name="behavior"/> <see cref="OccurenceUpdateBehavior.Group"/>
    /// </param>
    /// <param name="behavior">
    ///     Specifies occurrence update behavior
    /// </param>
    /// <exception cref="ArgumentException">
    ///     Throws when <paramref name="recurrence"/> or <paramref name="timeZone"/> is not null, but <paramref name="behavior"/> is not <see cref="OccurenceUpdateBehavior.Group"/>
    /// </exception>
    public UpdateOccurrenceArgs(
        string id,
        Optional<Period<DateTimeOffset>>? period = null,
        Optional<string>? timeZone = null,
        Optional<TData?>? data = null,
        Optional<Recurrence>? recurrence = null,
        OccurenceUpdateBehavior behavior = OccurenceUpdateBehavior.Occurrence)
    {
        if (behavior != OccurenceUpdateBehavior.Group && recurrence != null)
            throw new ArgumentException(
                $"Recurrence update can be specified only for {nameof(OccurenceUpdateBehavior.Group)} {nameof(behavior)}",
                nameof(recurrence));

        Id = OccurrenceId.Parse(id);
        Period = period;
        TimeZone = timeZone;
        Data = data;
        Behavior = behavior;
        Recurrence = recurrence;
    }

    /// <summary>
    ///     Creates new <see cref="UpdateOccurrenceArgs{TData}"/> for the move operation.
    /// </summary>
    /// <param name="id">Occurrence id. String representation of <see cref="OccurrenceId"/></param>
    /// <param name="newPeriod">New occurrence period</param>
    /// <param name="timeZone">New time zone</param>
    /// <param name="behavior"><see cref="OccurenceUpdateBehavior"/></param>
    /// <returns>Move instance of <see cref="UpdateOccurrenceArgs{TData}"/></returns>
    public static UpdateOccurrenceArgs<TData> NewMove(
        string id,
        Period<DateTimeOffset> newPeriod,
        string? timeZone = null,
        OccurenceUpdateBehavior behavior = OccurenceUpdateBehavior.Occurrence)
    {
        return new UpdateOccurrenceArgs<TData>(
            id,
            period: new Optional<Period<DateTimeOffset>>(newPeriod),
            timeZone: timeZone != null ? new Optional<string>(timeZone) : null,
            behavior: behavior);
    }

    public static UpdateOccurrenceArgs<TData> NewData(
        string id,
        TData? data,
        OccurenceUpdateBehavior behavior = OccurenceUpdateBehavior.Occurrence)
    {
        return new UpdateOccurrenceArgs<TData>(id, data: new Optional<TData?>(data), behavior: behavior);
    }
}