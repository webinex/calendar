using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using Webinex.Calendar.Common;
using Webinex.Calendar.DataAccess;
using Webinex.Calendar.Events;

namespace Webinex.Calendar.Filters;

public record EventFiltersProvider<TData>(
    DateTimeOffset From,
    DateTimeOffset To,
    Expression<Func<TData, bool>>? DataFilter,
    string TimeZone,
    bool OneTime,
    bool DayOfMonth,
    bool DayOfWeek,
    bool Interval,
    bool State,
    bool Data,
    bool Precise)
    where TData : class, ICloneable
{
    private static readonly Expression<Func<EventRow<TData>, bool>> BaseRecurrentEventStateFilter =
        x => x.Type == EventType.RecurrentEvent;

    private static readonly Expression<Func<EventRow<TData>, bool>> OneTimeEventFilterPredicate =
        x => x.Type == EventType.OneTimeEvent;

    private readonly Lazy<Expression<Func<EventRow<TData>, bool>>> _dataFilterLazy =
        new(() => Expressions.Child<EventRow<TData>, TData>(
            x => x.Data,
            DataFilter ?? throw new ArgumentNullException(nameof(DataFilter))));

    public bool Data { get; set; } = Data;
    public bool Precise { get; set; } = Precise;

    public Expression<Func<EventRow<TData>, bool>> Create()
    {
        if (!OneTime && !DayOfMonth && !DayOfWeek && !Interval)
            throw new ArgumentException(
                "All event Types are disabled. You must enable at least one of them (OneTime/DayOfMonth/DayOfWeek/Interval)");

        Expression<Func<EventRow<TData>, bool>> @base = x =>
            x.Effective.Start < To.TotalMinutesSince1990() &&
            (x.Effective.End > From.TotalMinutesSince1990() || x.Effective.End == null);

        if (TryCreateDataFilter(out var dataFilter))
            @base = Expressions.And(@base, dataFilter);

        return TryCreateRecurrentEventStateFilter(out var recurrentEventStateFilter)
            ? Expressions.Or(BaseAndOneTimeOrRecurrentEvent(), recurrentEventStateFilter)
            : BaseAndOneTimeOrRecurrentEvent();

        Expression<Func<EventRow<TData>, bool>> BaseAndOneTimeOrRecurrentEvent()
        {
            var oneTimeOrRecurrentEvent = OneTimeOrRecurrentEvent();
            return oneTimeOrRecurrentEvent == null ? @base : Expressions.And(@base, oneTimeOrRecurrentEvent);
        }

        Expression<Func<EventRow<TData>, bool>>? OneTimeOrRecurrentEvent()
        {
            var hasOneTimeFilter = TryCreateOneTimeEventFilter(out var oneTimeFilter);
            var hasRecurrentEventFilter = TryCreateRecurrentEventFilter(out var recurrentEventFilter);

            if (!hasOneTimeFilter && !hasRecurrentEventFilter)
                return null;

            if (oneTimeFilter != null && recurrentEventFilter != null)
                return Expressions.Or(oneTimeFilter, recurrentEventFilter);

            return oneTimeFilter ?? recurrentEventFilter;
        }
    }

    [MemberNotNullWhen(true, nameof(DataFilter))]
    private bool TryCreateDataFilter([NotNullWhen(true)] out Expression<Func<EventRow<TData>, bool>>? filter)
    {
        filter = null;
        if (!Data || DataFilter == null)
            return false;

        filter = _dataFilterLazy.Value;
        return true;
    }

    private bool TryCreateOneTimeEventFilter([NotNullWhen(true)] out Expression<Func<EventRow<TData>, bool>>? filter)
    {
        filter = null;
        if (!OneTime)
            return false;

        filter = OneTimeEventFilterPredicate;
        return true;
    }

    private bool TryCreateRecurrentEventStateFilter(
        [NotNullWhen(true)] out Expression<Func<EventRow<TData>, bool>>? filter)
    {
        filter = null;

        if (!State)
            return false;

        Expression<Func<EventRow<TData>, bool>> periodPredicate = x =>
            x.Type == EventType.RecurrentEventState && (
                (x.Effective.Start < To.TotalMinutesSince1990() &&
                 x.Effective.End!.Value > From.TotalMinutesSince1990())
                || (x.MoveTo != null && x.MoveTo.Start < To && x.MoveTo.End > From));

        if (!TryCreateDataFilter(out var dataFilter))
        {
            filter = periodPredicate;
            return true;
        }

        // We need this predicate here, because we might have in RecurrentEvent.Data value "1", but in RecurrentEventState.Data value "4"
        // and search by "1". In this case we should get nothing, because "4" overrides RecurrentEvent.Data. In this case we have to return to client both values
        // RecurrentEvent and RecurrentEventState
        var datePredicate = Expressions.Or(
            dataFilter,
            Expressions.Child<EventRow<TData>, TData>(x => x.RecurrentEvent!.Data, DataFilter!));

        filter = Expressions.And(periodPredicate, datePredicate);
        return true;
    }

    private bool TryCreateRecurrentEventFilter([NotNullWhen(true)] out Expression<Func<EventRow<TData>, bool>>? filter)
    {
        filter = null;
        if (!Precise && (DayOfWeek || DayOfMonth || Interval))
        {
            filter = BaseRecurrentEventStateFilter;
            return true;
        }

        var predicates = GetPredicates().ToArray();

        if (!predicates.Any())
            return false;

        filter = Expressions.And(BaseRecurrentEventStateFilter, Expressions.Or(predicates));
        return true;

        IEnumerable<Expression<Func<EventRow<TData>, bool>>> GetPredicates()
        {
            if (DayOfWeek)
                yield return new MatchWeekdayEventFilterFactory<TData>(From, To, TimeZone).Create();

            if (DayOfMonth)
                yield return new MatchDayOfMonthEventFilterFactory<TData>(From, To).Create();

            if (Interval)
                yield return CreateIntervalEventFilter();
        }
    }

    private Expression<Func<EventRow<TData>, bool>> CreateIntervalEventFilter()
    {
        var rangeMinutes = To.TotalMinutesSince1990() - From.TotalMinutesSince1990();

        return x => x.Repeat!.Type == EventRowRepeatType.Interval && (
                        x.Effective.Start >= From.TotalMinutesSince1990()
                        || (rangeMinutes >= x.Repeat.Interval &&
                            (!x.Effective.End.HasValue ||
                             x.Effective.End.Value >= To.TotalMinutesSince1990() ||
                             x.Effective.End.Value - From.TotalMinutesSince1990() >= x.Repeat.Interval))
                        || (From.TotalMinutesSince1990() - x.Effective.Start) %
                        x.Repeat.Interval < x.Repeat.DurationMinutes)

                    // TODO: s.skalaban check predicate
                    || (((x.Effective.Start - From.TotalMinutesSince1990()) % x.Repeat.Interval) +
                        x.Repeat.Interval + From.TotalMinutesSince1990() < To.TotalMinutesSince1990());
    }
}