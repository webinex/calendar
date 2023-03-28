using System.Linq.Expressions;
using Webinex.Calendar.Common;
using Webinex.Calendar.DataAccess;

namespace Webinex.Calendar.Filters;

public class MatchDayOfMonthEventFilterFactory<TData>
    where TData : class, ICloneable
{
    private const int MAX_DAYS_IN_MONTH = 31;

    private readonly DateTimeOffset _from;
    private readonly DateTimeOffset _to;

    public MatchDayOfMonthEventFilterFactory(DateTimeOffset from, DateTimeOffset to)
    {
        _from = from;
        _to = to;
    }

    private int ToDay => _to.Day;
    private int FromDay => _from.Day;
    private int DayBeforeFrom => _from.AddDays(-1).Day;
    private int[] WholeDays => DateTimeOffsetUtil.GetUniqueUtcWholeDayOfMonthInRange(_from, _to);

    public Expression<Func<EventRow<TData>, bool>> Create()
    {
        Expression<Func<EventRow<TData>, bool>> @base = x => x.Repeat!.Type == EventRowRepeatType.DayOfMonth;

        if (WholeDays.Length >= MAX_DAYS_IN_MONTH)
            return @base;

        return Expressions.And(
            @base,
            new[]
            {
                CreateDayBeforeOvernightExpression(),
                CreateFromDayExpression(),
                CreateWholeDayExpression(),
                CreateToDayExpression(),
            }.NotNull().Aggregate(Expressions.Or));
    }

    private Expression<Func<EventRow<TData>, bool>>? CreateDayBeforeOvernightExpression()
    {
        if (WholeDays.Contains(DayBeforeFrom))
            return null;

        return x => _from.TotalMinutesFromStartOfTheDayUtc() < x.Repeat!.OvernightDurationMinutes
                    && x.Repeat.DayOfMonth!.Value == DayBeforeFrom;
    }

    private Expression<Func<EventRow<TData>, bool>>? CreateWholeDayExpression()
    {
        if (!WholeDays.Any())
            return null;

        return x => WholeDays.Contains(x.Repeat!.DayOfMonth!.Value);
    }

    private Expression<Func<EventRow<TData>, bool>>? CreateFromDayExpression()
    {
        if (WholeDays.Contains(FromDay))
            return null;

        Expression<Func<EventRow<TData>, bool>> expression = x =>
            _from.TotalMinutesFromStartOfTheDayUtc() < x.Repeat!.SameDayLastTime &&
            x.Repeat.DayOfMonth!.Value == FromDay;

        if (_from.Date == _to.Date)
        {
            expression = Expressions.And(expression,
                x => _to.TotalMinutesFromStartOfTheDayUtc() > x.Repeat!.TimeOfTheDayUtcMinutes);
        }

        return expression;
    }

    private Expression<Func<EventRow<TData>, bool>>? CreateToDayExpression()
    {
        if (WholeDays.Contains(ToDay) || _from.Date == _to.Date)
            return null;

        Expression<Func<EventRow<TData>, bool>> expression = x =>
            _to.TotalMinutesFromStartOfTheDayUtc() > x.Repeat!.TimeOfTheDayUtcMinutes &&
            x.Repeat.DayOfMonth!.Value == ToDay;

        return expression;
    }
}