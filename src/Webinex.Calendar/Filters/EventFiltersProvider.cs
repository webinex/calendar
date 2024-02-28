using System.Linq.Expressions;
using Webinex.Calendar.Common;
using Webinex.Calendar.DataAccess;
using Webinex.Calendar.Events;

namespace Webinex.Calendar.Filters;

public class EventFiltersProvider<TData> where TData : class, ICloneable
{
    public DateTimeOffset From { get; init; }
    public DateTimeOffset To { get; init; }
    public Expression<Func<TData, bool>>? DataFilter { get; init; }
    public bool OneTime { get; init; }
    public bool DayOfMonth { get; init; }
    public bool DayOfWeek { get; init; }
    public bool Interval { get; init; }
    public bool State { get; init; }
    public bool Data { get; set; }
    public bool Precise { get; set; }

    public Expression<Func<EventRow<TData>, bool>> Create()
    {
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

        return x => x.Effective.End > From.TotalMinutesSince1990() && x.Type == EventType.OneTimeEvent;
    }

    private Expression<Func<EventRow<TData>, bool>>? CreateRecurrentEventStateFilter()
    {
        if (!State)
            return null;

        Expression<Func<EventRow<TData>, bool>> expression = x =>
            x.Type == EventType.RecurrentEventState && (
                (x.Effective.Start < To.TotalMinutesSince1990() &&
                 x.Effective.End!.Value > From.TotalMinutesSince1990())
                || (x.MoveTo != null && x.MoveTo.Start < To && x.MoveTo.End > From));

        if (!Data || DataFilter == null)
            return expression;

        return Expressions.And(
            expression,
            Expressions.Or(
                Expressions.Child<EventRow<TData>, TData>(x => x.Data, DataFilter),
                Expressions.Child<EventRow<TData>, TData>(x => x.RecurrentEvent!.Data, DataFilter)));
    }

    private Expression<Func<EventRow<TData>, bool>>? CreateRecurrentEventFilter()
    {
        Expression<Func<EventRow<TData>, bool>> @base = x => x.Type == EventType.RecurrentEvent;

        if (!Precise && (DayOfWeek || DayOfMonth || Interval))
            return Expressions.And(@base, x => x.Effective.Start < To.TotalMinutesSince1990() &&
                                               (x.Effective.End > From.TotalMinutesSince1990() ||
                                                x.Effective.End == null));

        var predicates = GetPredicates().ToArray();

        if (!predicates.Any())
            return null;

        return Expressions.And(@base, Expressions.Or(predicates));

        IEnumerable<Expression<Func<EventRow<TData>, bool>>> GetPredicates()
        {
            if (DayOfWeek)
                yield return new MatchWeekdayEventFilterFactory<TData>(From, To).Create();

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
                        || (rangeMinutes >= x.Repeat!.IntervalMinutes! &&
                            (!x.Effective.End.HasValue ||
                             x.Effective.End.Value >= To.TotalMinutesSince1990() ||
                             x.Effective.End.Value - From.TotalMinutesSince1990() >= x.Repeat.IntervalMinutes!))
                        || (From.TotalMinutesSince1990() - x.Effective.Start) %
                        x.Repeat.IntervalMinutes < x.Repeat.DurationMinutes)

                    // TODO: s.skalaban check predicate
                    || (((x.Effective.Start - From.TotalMinutesSince1990()) % x.Repeat.IntervalMinutes) +
                        x.Repeat.IntervalMinutes + From.TotalMinutesSince1990() < To.TotalMinutesSince1990());
    }
}