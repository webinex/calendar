using System.Linq.Expressions;
using NodaTime;
using NodaTime.Extensions;
using Webinex.Calendar.Common;
using Webinex.Calendar.DataAccess;
using Period = Webinex.Calendar.Common.Period;

namespace Webinex.Calendar.Filters;

internal class MatchWeekdayEventFilterFactory<TData>
    where TData : class, ICloneable
{
    private static readonly Expression<Func<EventRow<TData>, bool>> TypeMatchExpression =
        x => x.Repeat!.Type == EventRowRepeatType.Weekday;

    private readonly DateTimeOffset _from;
    private readonly DateTimeOffset _to;
    private readonly Lazy<Weekday[]> _fullDayWeekdays;

    public MatchWeekdayEventFilterFactory(DateTimeOffset from, DateTimeOffset to, string timeZone)
    {
        var tz = DateTimeZoneProviders.Tzdb[timeZone];
        _from = from.ToInstant().InZone(tz).ToDateTimeUnspecified();
        _to = to.ToInstant().InZone(tz).ToDateTimeUnspecified();
        _fullDayWeekdays = new Lazy<Weekday[]>(() => new Period(_from, _to).FullDayWeekdays());
    }

    private Weekday ToWeekday => Weekday.From(_to.DayOfWeek);
    private Weekday FromWeekday => Weekday.From(_from.DayOfWeek);
    private Weekday DayBeforeFromWeekday => Weekday.From(_from.AddDays(-1).DayOfWeek);
    private Weekday[] FullDayWeekdays => _fullDayWeekdays.Value;

    public Expression<Func<EventRow<TData>, bool>> Create()
    {
        // If in search period we have all weekdays we only need add filter by Repeat.Type,
        // because any weekday repetition is implicitly included.
        if (FullDayWeekdays.Length == Weekday.DAYS_IN_WEEK)
            return TypeMatchExpression;

        return Expressions.And(TypeMatchExpression, Expressions.Or(new[]
        {
            CreateWholeDayExpression(),
            CreateDayBeforeOvernightExpression(),
            CreateFromDayExpression(),
            CreateToDayExpression(),
        }.NotNull()));
    }

    private Expression<Func<EventRow<TData>, bool>>? CreateDayBeforeOvernightExpression()
    {
        if (FullDayWeekdays.Contains(DayBeforeFromWeekday))
            return null;

        Expression<Func<EventRow<TData>, bool>> weekdayExpression = x => _from.TotalMinutesFromStartOfTheDay() <
                                                                         x.Repeat!.OvernightDurationMinutes;

        return Expressions.And(weekdayExpression, EventRow<TData>.WeekdaySelector(DayBeforeFromWeekday));
    }

    private Expression<Func<EventRow<TData>, bool>>? CreateWholeDayExpression()
    {
        if (!FullDayWeekdays.Any())
            return null;

        return Expressions.Or(FullDayWeekdays.Select(EventRow<TData>.WeekdaySelector));
    }

    private Expression<Func<EventRow<TData>, bool>>? CreateFromDayExpression()
    {
        if (FullDayWeekdays.Contains(FromWeekday))
            return null;

        Expression<Func<EventRow<TData>, bool>> expression = x =>
            _from.TotalMinutesFromStartOfTheDay() < x.Repeat!.SameDayLastTime;

        if (_from.Date == _to.Date)
        {
            expression = Expressions.And(expression,
                x => _to.TotalMinutesFromStartOfTheDay() > x.Repeat!.TimeOfTheDayInMinutes);
        }

        return Expressions.And(expression, EventRow<TData>.WeekdaySelector(FromWeekday));
    }

    private Expression<Func<EventRow<TData>, bool>>? CreateToDayExpression()
    {
        if (FullDayWeekdays.Contains(ToWeekday) || _from.Date == _to.Date)
            return null;

        Expression<Func<EventRow<TData>, bool>> expression = x =>
            _to.TotalMinutesFromStartOfTheDay() > x.Repeat!.TimeOfTheDayInMinutes;

        return Expressions.And(expression, EventRow<TData>.WeekdaySelector(ToWeekday));
    }
}