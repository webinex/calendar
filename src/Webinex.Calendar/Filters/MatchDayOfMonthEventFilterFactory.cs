using System.Linq.Expressions;
using Webinex.Calendar.Common;
using Webinex.Calendar.DataAccess;

namespace Webinex.Calendar.Filters;

public class MatchDayOfMonthEventFilterFactory<TData>
    where TData : class, ICloneable
{
    private const int MAX_DAYS_IN_MONTH = 31;
    
    private static readonly Expression<Func<EventRow<TData>, bool>> TypeMatchExpression =
        x => x.Repeat!.Type == EventRowRepeatType.DayOfMonth;

    private readonly DateTimeOffset _from;
    private readonly DateTimeOffset _to;
    private readonly Lazy<int[]> _wholeDays;

    public MatchDayOfMonthEventFilterFactory(DateTimeOffset from, DateTimeOffset to)
    {
        _from = from;
        _to = to;
        _wholeDays = new Lazy<int[]>(() => DateTimeOffsetUtil.GetUniqueUtcWholeDayOfMonthInRange(_from, _to));
    }

    private int ToDay => _to.Day;
    private int FromDay => _from.Day;
    private int DayBeforeFrom => _from.AddDays(-1).Day;
    private int[] WholeDays => _wholeDays.Value;

    public Expression<Func<EventRow<TData>, bool>> Create()
    {
        if (WholeDays.Length >= MAX_DAYS_IN_MONTH)
            return TypeMatchExpression;

        return Expressions.And(
            TypeMatchExpression,
            Expressions.Or(new[]
            {
                CreateDayBeforeOvernightExpression(),
                CreateFromDayExpression(),
                CreateWholeDayExpression(),
                CreateToDayExpression(),
            }.NotNull()));
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
                x => _to.TotalMinutesFromStartOfTheDayUtc() > x.Repeat!.TimeOfTheDayInMinutes);
        }

        return expression;
    }

    private Expression<Func<EventRow<TData>, bool>>? CreateToDayExpression()
    {
        if (WholeDays.Contains(ToDay) || _from.Date == _to.Date)
            return null;

        Expression<Func<EventRow<TData>, bool>> expression = x =>
            _to.TotalMinutesFromStartOfTheDayUtc() > x.Repeat!.TimeOfTheDayInMinutes &&
            x.Repeat.DayOfMonth!.Value == ToDay;

        return expression;
    }
}