using System.Linq.Expressions;
using Webinex.Calendar.Common;
using Webinex.Calendar.DataAccess;

namespace Webinex.Calendar.Filters;

internal class MatchWeekdayEventFilterFactory<TData>
    where TData : class, ICloneable
{
    private readonly DateTimeOffset _from;
    private readonly DateTimeOffset _to;

    public MatchWeekdayEventFilterFactory(DateTimeOffset from, DateTimeOffset to)
    {
        _from = from.ToUtc();
        _to = to.ToUtc();
    }

    private Weekday ToWeekday => Weekday.From(_to.DayOfWeek);
    private Weekday FromWeekday => Weekday.From(_from.DayOfWeek);
    private Weekday DayBeforeFromWeekday => Weekday.From(_from.AddDays(-1).DayOfWeek);
    private Weekday[] WholeDayWeekdays => DateTimeOffsetUtil.GetUniqueUtcWholeWeekdaysInRange(_from, _to);

    public Expression<Func<EventRow<TData>, bool>> Create()
    {
        Expression<Func<EventRow<TData>, bool>> typeMatchExpression =
            x => x.Repeat!.Type == EventRowRepeatType.Weekday;

        return Expressions.And(typeMatchExpression, new[]
        {
            CreateWholeDayExpression(),
            CreateDayBeforeOvernightExpression(),
            CreateFromDayExpression(),
            CreateToDayExpression(),
        }.NotNull().Aggregate(Expressions.Or));
    }

    private Expression<Func<EventRow<TData>, bool>>? CreateDayBeforeOvernightExpression()
    {
        if (WholeDayWeekdays.Contains(DayBeforeFromWeekday))
            return null;

        Expression<Func<EventRow<TData>, bool>> weekdayExpression = x => _from.TotalMinutesFromStartOfTheDayUtc() <
                                                                         x.Repeat!.OvernightDurationMinutes;

        return Expressions.And(weekdayExpression, EventRow<TData>.Selector(DayBeforeFromWeekday));
    }

    private Expression<Func<EventRow<TData>, bool>>? CreateWholeDayExpression()
    {
        return !WholeDayWeekdays.Any()
            ? null
            : WholeDayWeekdays.Select(EventRow<TData>.Selector).NotNull().Aggregate(Expressions.Or);
    }

    private Expression<Func<EventRow<TData>, bool>>? CreateFromDayExpression()
    {
        if (WholeDayWeekdays.Contains(FromWeekday))
            return null;

        Expression<Func<EventRow<TData>, bool>> expression = x =>
            _from.TotalMinutesFromStartOfTheDayUtc() < x.Repeat!.SameDayLastTime;

        if (_from.Date == _to.Date)
        {
            expression = Expressions.And(expression,
                x => _to.TotalMinutesFromStartOfTheDayUtc() > x.Repeat!.TimeOfTheDayUtcMinutes);
        }

        return Expressions.And(expression, EventRow<TData>.Selector(FromWeekday));
    }

    private Expression<Func<EventRow<TData>, bool>>? CreateToDayExpression()
    {
        if (WholeDayWeekdays.Contains(ToWeekday) || _from.Date == _to.Date)
            return null;

        Expression<Func<EventRow<TData>, bool>> expression = x =>
            _to.TotalMinutesFromStartOfTheDayUtc() > x.Repeat!.TimeOfTheDayUtcMinutes;

        return Expressions.And(expression, EventRow<TData>.Selector(ToWeekday));
    }
}