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

        if (Data && DataFilter != null)
            @base = Expressions.And(@base, Expressions.Child<EventRow<TData>, TData>(x => x.Data, DataFilter));

        return Expressions.Or(
            Expressions.And(@base,
                Expressions.Or(OneTimeEventFilter() ?? (_ => false), CreateRecurrentEventFilter() ?? (_ => false))),
            CreateRecurrentEventStateFilter() ?? (_ => false));
    }

    private Expression<Func<EventRow<TData>, bool>>? OneTimeEventFilter()
    {
        if (!OneTime)
            return null;

        return x => x.Type == EventType.OneTimeEvent;
    }

    private Expression<Func<EventRow<TData>, bool>>? CreateRecurrentEventStateFilter()
    {
        if (!State)
            return null;

        Expression<Func<EventRow<TData>, bool>> periodPredicate = x =>
            x.Type == EventType.RecurrentEventState && (
                (x.Effective.Start < To.TotalMinutesSince1990() &&
                 x.Effective.End!.Value > From.TotalMinutesSince1990())
                || (x.MoveTo != null && x.MoveTo.Start < To && x.MoveTo.End > From));

        if (!Data || DataFilter == null)
            return periodPredicate;

        // We need this predicate here, because we might have in RecurrentEvent.Data value "1", but in RecurrentEventState.Data value "4"
        // and search by "1". In this case we should get nothing, because "4" overrides RecurrentEvent.Data. In this case we have to return to client both values
        // RecurrentEvent and RecurrentEventState
        var datePredicate = Expressions.Or(
            Expressions.Child<EventRow<TData>, TData>(x => x.Data, DataFilter),
            Expressions.Child<EventRow<TData>, TData>(x => x.RecurrentEvent!.Data, DataFilter));

        return Expressions.And(periodPredicate, datePredicate);
    }

    private Expression<Func<EventRow<TData>, bool>>? CreateRecurrentEventFilter()
    {
        Expression<Func<EventRow<TData>, bool>> @base = x => x.Type == EventType.RecurrentEvent;

        if (!Precise && (DayOfWeek || DayOfMonth || Interval))
            return @base;

        var predicates = GetPredicates().ToArray();

        if (!predicates.Any())
            return null;

        return Expressions.And(@base, Expressions.Or(predicates));

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
                        || (rangeMinutes >= x.Repeat!.Interval! &&
                            (!x.Effective.End.HasValue ||
                             x.Effective.End.Value >= To.TotalMinutesSince1990() ||
                             x.Effective.End.Value - From.TotalMinutesSince1990() >= x.Repeat.Interval!))
                        || (From.TotalMinutesSince1990() - x.Effective.Start) %
                        x.Repeat.Interval < x.Repeat.DurationMinutes)

                    // TODO: s.skalaban check predicate
                    || (((x.Effective.Start - From.TotalMinutesSince1990()) % x.Repeat.Interval) +
                        x.Repeat.Interval + From.TotalMinutesSince1990() < To.TotalMinutesSince1990());
    }
}