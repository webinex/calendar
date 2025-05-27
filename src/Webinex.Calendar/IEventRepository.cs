using Webinex.Asky;
using Webinex.Calendar.Common;

namespace Webinex.Calendar;

public interface IEventRepository<TData>
    where TData : class, ICloneable
{
    /// <summary>
    ///     Returns <typeparamref name="T"/> by ids.
    /// </summary>
    /// <param name="ids">Identifiers of <typeparamref name="T"/></param>
    /// <typeparam name="T">
    ///     Discriminator of event record types. Might be <see cref="Event{TData}"/> or <see cref="OccurrenceAdjustment{TData}"/>
    /// </typeparam>
    /// <example>
    ///     <code>
    ///         var events = await _eventRepository.ByIdAsync&lt;Event&lt;TData&gt;&gt;(ids);
    ///         var states = await _eventRepository.ByIdAsync&lt;EventState&lt;TData&gt;&gt;(ids);
    ///         var both = await _eventRepository.ByIdAsync&lt;IEventBase&gt;(ids);
    ///     </code>
    /// </example>
    /// <returns>Collection of <typeparamref name="T"/> matched <paramref name="ids"/></returns>
    Task<IReadOnlyCollection<T>> ByIdAsync<T>(IEnumerable<string> ids)
        where T : IEventEntityBase;

    /// <summary>
    ///     Performs multiple operations as one operation.
    ///     Intended to be useful for non-EntityFramework repositories which doesn't implement Unit of Work pattern.
    /// </summary>
    /// <param name="operations">Patches to be applied against database</param>
    /// <returns>Patch-IEventBase key value pairs</returns>
    /// <example>
    ///     <code>
    ///         var result = await _eventRepository.PatchAsync([
    ///             Patch.Add(eventToAdd),
    ///             Patch.Update(eventToUpdate),
    ///             Patch.Remove(eventToRemove)]);
    ///     </code>
    /// </example>
    Task<IReadOnlyDictionary<Operation, IEventEntityBase>> PatchAsync(IEnumerable<Operation> operations);

    /// <summary>
    ///     Finds <see cref="IEventEntityBase"/> which matches <paramref name="period"/> and <see cref="dataFilterRule"/>.
    ///     The difference with <see cref="GetAllAsync{T}"/> in this method returns not only entities which
    ///     matches predicate, but also entities related to them, but not matched.
    ///     <br />
    ///     <br />
    ///
    ///     You are looking for data from 2021-01-01T00:00:00 until 2021-01-08T00:00:00, but you're still interested in:
    ///     <list type="bullet">
    ///         <item>If occurrence 2021-01-07T23:00:00 moved two hours later, it still interested for you, because you need to remove it from resulted occurrences</item>
    ///         <item>If occurrence moved back, it can appear in result, but original recurrent event not</item>
    ///         <item>If occurrence data doesn't match search criteria, but original recurrent event matches - you need it to exclude from result</item>
    ///     </list>
    /// </summary>
    /// <param name="period">Match period</param>
    /// <param name="dataFilterRule">Data match criteria</param>
    /// <returns>Matched entities</returns>
    Task<IReadOnlyCollection<IEventEntityBase>> MatchAsync(Period<DateTimeOffset> period, FilterRule? dataFilterRule = null);

    /// <summary>
    ///     Returns <typeparamref name="T"/> matched specified search criteria
    /// </summary>
    /// <param name="period"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    Task<IReadOnlyCollection<T>> GetAllAsync<T>(
        FilterRule? filterRule = null,
        IEnumerable<SortRule>? sortRules = null,
        PagingRule? pagingRule = null,
        bool readOnly = false)
        where T : IEventEntityBase;

    /// <summary>
    ///     Returns <typeparamref name="T"/> matched specified search criteria
    /// </summary>
    /// <param name="period"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    Task<bool> AnyAsync<T>(FilterRule? filterRule = null) where T : IEventEntityBase;

    /// <summary>
    ///     Returns <typeparamref name="T"/> matched specified search criteria
    /// </summary>
    /// <param name="period"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    Task<int> CountAsync<T>(FilterRule? filterRule = null) where T : IEventEntityBase;
    
    /// <summary>
    ///     Returns <see cref="EventGroup"/> collection by <paramref name="ids"/>
    /// </summary>
    /// <param name="ids">Identifiers of groups</param>
    /// <returns><see cref="EventGroup"/> collection</returns>
    Task<IReadOnlyCollection<EventGroup>> EventGroupAsync(IEnumerable<Guid> ids);
}

public static class EventRepositoryExtensions
{
    public static async Task<IReadOnlyCollection<Event<TData>>> EventAsync<TData>(
        this IEventRepository<TData> repository, IEnumerable<string> ids)
        where TData : class, ICloneable
    {
        return await repository.ByIdAsync<Event<TData>>(ids);
    }

    public static async Task<OccurrenceAdjustment<TData>?> OccurrenceAdjustmentAsync<TData>(
        this IEventRepository<TData> repository,
        string id)
        where TData : class, ICloneable
    {
        var result = await repository.ByIdAsync<OccurrenceAdjustment<TData>>([id]);
        return result.FirstOrDefault();
    }

    public static async Task<Event<TData>?> EventAsync<TData>(
        this IEventRepository<TData> repository, string id)
        where TData : class, ICloneable
    {
        var result = await repository.ByIdAsync<Event<TData>>([id]);
        return result.FirstOrDefault();
    }

    public static async Task<IReadOnlyCollection<IEvent<TData>>> AddRangeAsync<TData>(
        this IEventRepository<TData> repository,
        IEnumerable<IEvent<TData>> events)
        where TData : class, ICloneable
    {
        var result = await repository.PatchAsync(events.Select(Operation.Add));
        return result.Values.Cast<IEvent<TData>>().ToArray();
    }

    public static async Task<IReadOnlyCollection<OccurrenceAdjustment<TData>>> AddRangeAsync<TData>(
        this IEventRepository<TData> repository,
        IEnumerable<OccurrenceAdjustment<TData>> states)
        where TData : class, ICloneable
    {
        var result = await repository.PatchAsync(states.Select(Operation.Add));
        return result.Values.Cast<OccurrenceAdjustment<TData>>().ToArray();
    }

    public static async Task<IReadOnlyCollection<Event<TData>>> UpdateRangeAsync<TData>(
        this IEventRepository<TData> repository,
        IEnumerable<Event<TData>> events)
        where TData : class, ICloneable
    {
        var result = await repository.PatchAsync(events.Select(Operation.Update));
        return result.Values.Cast<Event<TData>>().ToArray();
    }

    public static async Task<IReadOnlyCollection<OccurrenceAdjustment<TData>>> UpdateRangeAsync<TData>(
        this IEventRepository<TData> repository,
        IEnumerable<OccurrenceAdjustment<TData>> states)
        where TData : class, ICloneable
    {
        var result = await repository.PatchAsync(states.Select(Operation.Update));
        return result.Values.Cast<OccurrenceAdjustment<TData>>().ToArray();
    }

    public static async Task<IReadOnlyCollection<Event<TData>>> RemoveRangeAsync<TData>(
        this IEventRepository<TData> repository,
        IEnumerable<Event<TData>> events)
        where TData : class, ICloneable
    {
        var result = await repository.PatchAsync(events.Select(Operation.Remove));
        return result.Values.Cast<Event<TData>>().ToArray();
    }

    public static async Task<IReadOnlyCollection<Event<TData>>> RemoveRangeAsync<TData>(
        this IEventRepository<TData> repository,
        IEnumerable<IEventEntityBase> events)
        where TData : class, ICloneable
    {
        var result = await repository.PatchAsync(events.Select(Operation.Remove));
        return result.Values.Cast<Event<TData>>().ToArray();
    }

    public static async Task<IReadOnlyCollection<OccurrenceAdjustment<TData>>> RemoveRangeAsync<TData>(
        this IEventRepository<TData> repository,
        IEnumerable<OccurrenceAdjustment<TData>> states)
        where TData : class, ICloneable
    {
        var result = await repository.PatchAsync(states.Select(Operation.Remove));
        return result.Values.Cast<OccurrenceAdjustment<TData>>().ToArray();
    }
}