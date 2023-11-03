using System.Linq.Expressions;
using Webinex.Calendar.Common;
using Webinex.Calendar.DataAccess;
using Webinex.Calendar.Events;

namespace Webinex.Calendar.Filters;

internal static class EventFilterFactory
{
    public static Expression<Func<EventRow<TData>, bool>> Create<TData>(
        DateTimeOffset from,
        DateTimeOffset to,
        Expression<Func<TData, bool>>? dataFilter,
        string timeZone)
        where TData : class, ICloneable
    {
        return new Factory<TData>(from, to, dataFilter, timeZone).Create();
    }

    private class Factory<TData>
        where TData : class, ICloneable
    {
        private readonly DateTimeOffset _from;
        private readonly DateTimeOffset _to;
        private readonly Expression<Func<TData, bool>>? _dataFilter;
        private readonly string _timeZone;

        public Factory(
            DateTimeOffset from,
            DateTimeOffset to,
            Expression<Func<TData, bool>>? dataFilter,
            string timeZone)
        {
            _dataFilter = dataFilter;
            _timeZone = timeZone;
            _from = from.ToUtc();
            _to = to.ToUtc();
        }

        public Expression<Func<EventRow<TData>, bool>> Create()
        {
            Expression<Func<EventRow<TData>, bool>> @base = x =>
                x.Effective.Start < _to.TotalMinutesSince1990() &&
                (x.Effective.End > _from.TotalMinutesSince1990() || x.Effective.End == null);

            if (_dataFilter != null)
                @base = Expressions.And(@base, Expressions.Child<EventRow<TData>, TData>(x => x.Data, _dataFilter));

            Expression<Func<EventRow<TData>, bool>> oneTimeEventFilter = x =>
                x.Effective.End > _from.TotalMinutesSince1990() && x.Type == EventType.OneTimeEvent;

            return Expressions.Or(
                Expressions.And(@base,
                    Expressions.Or(oneTimeEventFilter, CreateRecurrentEventFilter())),
                CreateRecurrentEventStateFilter());
        }

        private Expression<Func<EventRow<TData>, bool>> CreateRecurrentEventStateFilter()
        {
            Expression<Func<EventRow<TData>, bool>> expression = x =>
                x.Type == EventType.RecurrentEventState && (
                    (x.Effective.Start < _to.TotalMinutesSince1990() &&
                     x.Effective.End!.Value > _from.TotalMinutesSince1990())
                    || (x.MoveTo != null && x.MoveTo.Start < _to && x.MoveTo.End > _from)
                );

            if (_dataFilter == null)
                return expression;

            return Expressions.And(
                expression,
                Expressions.Or(
                    Expressions.Child<EventRow<TData>, TData>(x => x.Data, _dataFilter),
                    Expressions.Child<EventRow<TData>, TData>(x => x.RecurrentEvent!.Data, _dataFilter)));
        }

        private Expression<Func<EventRow<TData>, bool>> CreateRecurrentEventFilter()
        {
            Expression<Func<EventRow<TData>, bool>> @base = x => x.Type == EventType.RecurrentEvent;

            return Expressions.And(
                @base,
                Expressions.Or(
                    new MatchWeekdayEventFilterFactory<TData>(_from, _to, _timeZone).Create(),
                    new MatchDayOfMonthEventFilterFactory<TData>(_from, _to).Create(),
                    CreateIntervalEventFilter()));
        }

        private Expression<Func<EventRow<TData>, bool>> CreateIntervalEventFilter()
        {
            var rangeMinutes = _to.TotalMinutesSince1990() - _from.TotalMinutesSince1990();

            return x => x.Repeat!.Type == EventRowRepeatType.Interval && (
                            x.Effective.Start >= _from.TotalMinutesSince1990()
                            || (rangeMinutes >= x.Repeat!.Interval! &&
                                (!x.Effective.End.HasValue ||
                                 x.Effective.End.Value >= _to.TotalMinutesSince1990() ||
                                 x.Effective.End.Value - _from.TotalMinutesSince1990() >= x.Repeat.Interval!))
                            || (_from.TotalMinutesSince1990() - x.Effective.Start) %
                            x.Repeat.Interval < x.Repeat.DurationMinutes)

                        // TODO: s.skalaban check predicate
                        || (((x.Effective.Start - _from.TotalMinutesSince1990()) % x.Repeat.Interval) +
                            x.Repeat.Interval + _from.TotalMinutesSince1990() < _to.TotalMinutesSince1990());
        }
    }
}