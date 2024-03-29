﻿using System.Linq.Expressions;
using NodaTime;
using NodaTime.Extensions;
using Webinex.Calendar.Common;
using Webinex.Calendar.DataAccess;
using Period = Webinex.Calendar.Common.Period;

namespace Webinex.Calendar.Filters;

internal class MatchWeekdayEventFilterFactory<TData>
    where TData : class, ICloneable
{
    private readonly DateTimeOffset _from;
    private readonly DateTimeOffset _to;

    public MatchWeekdayEventFilterFactory(DateTimeOffset from, DateTimeOffset to, string timeZone)
    {
        var tz = DateTimeZoneProviders.Tzdb[timeZone];
        _from = from.ToInstant().InZone(tz).ToDateTimeUnspecified();
        _to = to.ToInstant().InZone(tz).ToDateTimeUnspecified();
    }

    private Weekday ToWeekday => Weekday.From(_to.DayOfWeek);
    private Weekday FromWeekday => Weekday.From(_from.DayOfWeek);
    private Weekday DayBeforeFromWeekday => Weekday.From(_from.AddDays(-1).DayOfWeek);
    private Weekday[] FullDayWeekdays => new Period(_from, _to).FullDayWeekdays();

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
        if (FullDayWeekdays.Contains(DayBeforeFromWeekday))
            return null;

        Expression<Func<EventRow<TData>, bool>> weekdayExpression = x => _from.TotalMinutesFromStartOfTheDay() <
                                                                         x.Repeat!.OvernightDurationMinutes;

        return Expressions.And(weekdayExpression, EventRow<TData>.Selector(DayBeforeFromWeekday));
    }

    private Expression<Func<EventRow<TData>, bool>>? CreateWholeDayExpression()
    {
        return !FullDayWeekdays.Any()
            ? null
            : FullDayWeekdays.Select(EventRow<TData>.Selector).NotNull().Aggregate(Expressions.Or);
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

        return Expressions.And(expression, EventRow<TData>.Selector(FromWeekday));
    }

    private Expression<Func<EventRow<TData>, bool>>? CreateToDayExpression()
    {
        if (FullDayWeekdays.Contains(ToWeekday) || _from.Date == _to.Date)
            return null;

        Expression<Func<EventRow<TData>, bool>> expression = x =>
            _to.TotalMinutesFromStartOfTheDay() > x.Repeat!.TimeOfTheDayInMinutes;

        return Expressions.And(expression, EventRow<TData>.Selector(ToWeekday));
    }
}