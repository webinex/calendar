using System.Linq.Expressions;
using Webinex.Calendar.DataAccess;
using Webinex.Calendar.Repeats;

namespace Webinex.Calendar.Filters;

internal class EventFilterFactory
{
    public static Expression<Func<EventRow<TData>, bool>> Create<TData>(
        DateTimeOffset from,
        DateTimeOffset to,
        Expression<Func<TData, bool>>? dataFilter)
        where TData : class, ICloneable
    {
        return new Factory<TData>(from, to, dataFilter).Create();
    }

    private class Factory<TData>
        where TData : class, ICloneable
    {
        private const int MAX_DAYS_IN_MONTH = 31;

        private readonly DateTimeOffset _from;
        private readonly DateTimeOffset _to;
        private readonly Expression<Func<TData, bool>>? _dataFilter;

        public Factory(DateTimeOffset from, DateTimeOffset to, Expression<Func<TData, bool>>? dataFilter)
        {
            _dataFilter = dataFilter;
            _from = from.ToUtc();
            _to = to.ToUtc();
        }

        public Expression<Func<EventRow<TData>, bool>> Create()
        {
            Expression<Func<EventRow<TData>, bool>> @base = x =>
                x.Effective.Start < _to && (x.Effective.End > _from || x.Effective.End == null);

            if (_dataFilter != null)
                @base = Expressions.And(@base, Expressions.Child<EventRow<TData>, TData>(x => x.Data, _dataFilter));

            Expression<Func<EventRow<TData>, bool>> oneTimeEventFilter = x =>
                x.Effective.End > _from && x.Type == EventRowType.OneTimeEvent;

            return Expressions.Or(
                Expressions.And(@base,
                    Expressions.Or(oneTimeEventFilter, CreateRecurrentEventFilter())),
                CreateRecurrentEventStateFilter());
        }

        private Expression<Func<EventRow<TData>, bool>> CreateRecurrentEventStateFilter()
        {
            Expression<Func<EventRow<TData>, bool>> expression = x =>
                x.Type == EventRowType.RecurrentEventState && (
                    (x.Effective.Start < _to && x.Effective.End!.Value > _from)
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
            Expression<Func<EventRow<TData>, bool>> @base = x => x.Type == EventRowType.RecurrentEvent;

            return Expressions.And(
                @base,
                Expressions.NullableOrOrNull(
                    CreateMatchEventFilter(),
                    CreateIntervalEventFilter())!);
        }

        private Expression<Func<EventRow<TData>, bool>> CreateIntervalEventFilter()
        {
            var fromSince1990Utc = (long)(_from - Constants.J1_1990).TotalMinutes;
            var toSince1990Utc = (long)(_to - Constants.J1_1990).TotalMinutes;
            var rangeMinutes = (_to - _from).TotalMinutes;

            return x => x.Repeat!.Type == EventRowRepeatType.Interval && (
                x.Repeat!.Interval!.StartSince1990Minutes >= fromSince1990Utc
                || (rangeMinutes >= x.Repeat!.Interval!.IntervalMinutes &&
                    (!x.Repeat.Interval.EndSince1990Minutes.HasValue ||
                     x.Repeat.Interval.EndSince1990Minutes.Value >= toSince1990Utc ||
                     x.Repeat.Interval.EndSince1990Minutes - fromSince1990Utc >= x.Repeat.Interval.IntervalMinutes))
                || (fromSince1990Utc - x.Repeat.Interval.StartSince1990Minutes) %
                x.Repeat.Interval.IntervalMinutes < x.Repeat.Interval.DurationMinutes);
        }

        private Expression<Func<EventRow<TData>, bool>>? CreateMatchEventFilter()
        {
            Expression<Func<EventRow<TData>, bool>> matchRepeatFilter = x => x.Repeat!.Type == EventRowRepeatType.Match;

            var filter = Expressions.NullableAndOrNull(
                CreateWeekdayRepeatFilter(),
                CreateDayOfMonthRepeatFilter());

            return filter != null ? Expressions.And(matchRepeatFilter, filter) : null;
        }

        private Expression<Func<EventRow<TData>, bool>>? CreateDayOfMonthRepeatFilter()
        {
            var daysOfMonth = DateTimeOffsetUtil.GetUniqueDayOfMonthInRange(_from, _to);
            return daysOfMonth.Length >= MAX_DAYS_IN_MONTH
                ? null
                : x => x.Repeat!.Match!.DayOfMonth == null || daysOfMonth.Contains(x.Repeat!.Match!.DayOfMonth!.Value);
        }

        private Expression<Func<EventRow<TData>, bool>>? CreateWeekdayRepeatFilter()
        {
            Expression<Func<EventRow<TData>, bool>> @base = x => x.Repeat!.Type == EventRowRepeatType.Match;

            var filter = Expressions.NullableOrOrNull(
                CreateWholeWeekdayEventFilter(),
                CreateOvernightWeekdayEventFilter(),
                CreateSameDayEventFilter(),
                CreateLastDayEventFilter());

            return filter != null ? Expressions.And(@base, filter) : null;
        }

        private Expression<Func<EventRow<TData>, bool>>? CreateOvernightWeekdayEventFilter()
        {
            var wholeDayWeekdays = DateTimeOffsetUtil.GetUniqueUtcWholeWeekdaysInRange(_from, _to);
            var weekdayBeforeFrom = Weekday.From(_from.AddDays(-1).DayOfWeek);

            if (wholeDayWeekdays.Contains(weekdayBeforeFrom))
                return null;

            Expression<Func<EventRow<TData>, bool>> weekdayExpression = x => _from.TotalMinutesFromStartOfTheDayUtc() <
                                                                             x.Repeat!.Match!.OvernightDurationMinutes;

            return Expressions.And(weekdayExpression, WeekdayExpression(weekdayBeforeFrom));
        }

        private Expression<Func<EventRow<TData>, bool>>? CreateWholeWeekdayEventFilter()
        {
            var wholeDayWeekdays = DateTimeOffsetUtil.GetUniqueUtcWholeWeekdaysInRange(_from, _to);

            return wholeDayWeekdays.Aggregate(default(Expression<Func<EventRow<TData>, bool>>),
                (expr, weekday) =>
                    Expressions.NullableOrOrNull(expr, WeekdayExpression(weekday)));
        }

        private Expression<Func<EventRow<TData>, bool>>? CreateSameDayEventFilter()
        {
            var wholeDayWeekdays = DateTimeOffsetUtil.GetUniqueUtcWholeWeekdaysInRange(_from, _to);
            var fromWeekday = Weekday.From(_from.DayOfWeek);

            if (wholeDayWeekdays.Contains(fromWeekday))
                return null;

            Expression<Func<EventRow<TData>, bool>> expression = x =>
                _from.TotalMinutesFromStartOfTheDayUtc() < x.Repeat!.Match!.SameDayLastTime &&
                _to.TotalMinutesFromStartOfTheDayUtc() > x.Repeat!.Match!.TimeOfTheDayUtcMinutes;
            return Expressions.And(expression, WeekdayExpression(fromWeekday));
        }

        private Expression<Func<EventRow<TData>, bool>>? CreateLastDayEventFilter()
        {
            var wholeDayWeekdays = DateTimeOffsetUtil.GetUniqueUtcWholeWeekdaysInRange(_from, _to);
            var toWeekday = Weekday.From(_to.DayOfWeek);

            if (wholeDayWeekdays.Contains(toWeekday))
                return null;

            Expression<Func<EventRow<TData>, bool>> expression = x =>
                _to.TotalMinutesFromStartOfTheDayUtc() > x.Repeat!.Match!.TimeOfTheDayUtcMinutes &&
                _from.TotalMinutesFromStartOfTheDayUtc() < x.Repeat.Match.SameDayLastTime;
            return Expressions.And(expression, WeekdayExpression(toWeekday));
        }

        private Expression<Func<EventRow<TData>, bool>> WeekdayExpression(Weekday weekday)
        {
            return Expressions.Eq(EventRow<TData>.Selector(weekday), true);
        }
    }
}