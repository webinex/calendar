using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Webinex.Calendar.Common;
using Webinex.Calendar.DataAccess;
using Webinex.Calendar.Events;
using Webinex.Calendar.Filters;

namespace Webinex.Calendar.Tests.EventFilterFactoryTests;

public class EventFilterFactoryScenario
{
    private readonly Dictionary<string, List<EventRow<TestEventData>>> _events = new();
    private OpenPeriod _period = null!;

    public EventFilterFactoryScenario WithRange(DateTimeOffset start, DateTimeOffset end)
    {
        _period = new OpenPeriod(start, end);
        return this;
    }

    public EventFilterFactoryScenario WithOneTimeEvent(DateTimeOffset start, string duration)
    {
        return WithOneTimeEvent(Guid.NewGuid().ToString(), start, duration);
    }

    public EventFilterFactoryScenario WithDayOfMonthRepeatEvent(
        string timeOfTheDay,
        string duration,
        int dayOfMonth)
    {
        return WithDayOfMonthRepeatEvent(Guid.NewGuid().ToString(), timeOfTheDay, duration, dayOfMonth);
    }

    public EventFilterFactoryScenario WithDayOfMonthRepeatEvent(
        string tag,
        string timeOfTheDay,
        string duration,
        int dayOfMonth)
    {
        var @event = RecurrentEvent<TestEventData>.NewDayOfMonth(
            Constants.J1_1990,
            null,
            (int)TimeSpan.Parse(timeOfTheDay).TotalMinutes,
            (int)TimeSpan.Parse(duration).TotalMinutes,
            new DayOfMonth(dayOfMonth),
            TimeZoneInfo.Utc.Id,
            new TestEventData());
        var row = new EventRow<TestEventData>(Guid.NewGuid(), @event.Effective, EventType.RecurrentEvent,
            EventRowRepeat.From(@event.Repeat), null, @event.Data, null, false);

        Add(tag, row);
        return this;
    }

    public EventFilterFactoryScenario WithWeekdayRepeatEvent(
        string timeOfTheDay,
        string duration,
        params Weekday[] weekdays)
    {
        return WithWeekdayRepeatEvent(Guid.NewGuid().ToString(), timeOfTheDay, duration, weekdays);
    }

    public EventFilterFactoryScenario WithWeekdayRepeatEvent(
        string tag,
        string timeOfTheDay,
        string duration,
        params Weekday[] weekdays)
    {
        var @event = RecurrentEvent<TestEventData>.NewWeekday(
            Constants.J1_1990,
            null,
            (int)TimeSpan.Parse(timeOfTheDay).TotalMinutes,
            (int)TimeSpan.Parse(duration).TotalMinutes,
            weekdays,
            TimeZoneInfo.Utc.Id,
            new TestEventData());
        var row = new EventRow<TestEventData>(Guid.NewGuid(), @event.Effective, EventType.RecurrentEvent,
            EventRowRepeat.From(@event.Repeat), null, @event.Data, null, false);

        Add(tag, row);
        return this;
    }

    public EventFilterFactoryScenario WithIntervalRepeatEvent(
        DateTimeOffset start,
        string interval,
        string duration)
    {
        return WithIntervalRepeatEvent(Guid.NewGuid().ToString(), start, interval, duration);
    }

    public EventFilterFactoryScenario WithIntervalRepeatEvent(
        string tag,
        DateTimeOffset start,
        string interval,
        string duration)
    {
        var @event = RecurrentEvent<TestEventData>.NewInterval(
            start,
            null,
            (int)TimeSpan.Parse(interval).TotalMinutes,
            (int)TimeSpan.Parse(duration).TotalMinutes,
            new TestEventData());

        var row = new EventRow<TestEventData>(Guid.NewGuid(), @event.Effective, EventType.RecurrentEvent,
            EventRowRepeat.From(@event.Repeat), null, @event.Data, null, false);

        Add(tag, row);
        return this;
    }

    public EventFilterFactoryScenario WithOneTimeEvent(string tag, DateTimeOffset start, string duration)
    {
        var @event = OneTimeEvent<TestEventData>.New(new Period(start, start.Add(duration)), new TestEventData());
        Add(tag, EventRow.From(@event));

        return this;
    }

    public EventFilterFactoryScenario WithExactDateEvent(
        EventType type,
        DateTimeOffset start,
        string duration)
    {
        return WithExactDateEvent(Guid.NewGuid().ToString(), type, start, duration);
    }

    public EventFilterFactoryScenario WithExactDateEvent(
        string tag,
        EventType type,
        DateTimeOffset start,
        string duration)
    {
        var @event = MapExactDateEvent(type, start, duration);
        Add(tag, @event);

        return this;
    }

    private EventRow<TestEventData> MapExactDateEvent(EventType type, DateTimeOffset start, string duration)
    {
        switch (type)
        {
            case EventType.OneTimeEvent:
            {
                var @event =
                    OneTimeEvent<TestEventData>.New(new Period(start, start.Add(duration)), new TestEventData());
                return EventRow.From(@event);
            }

            case EventType.RecurrentEventState:
            {
                return EventRow<TestEventData>.NewRecurrentEventState(Guid.NewGuid(), start, start.Add(duration),
                    new TestEventData(), null, false);
            }

            default:
                throw new InvalidOperationException();
        }
    }

    public void ToContainAll()
    {
        ToContain(_events.Select(x => x.Key).ToArray());
    }

    public void ToContain(params string[] tags)
    {
        var events = _events
            .Where(x => tags.Contains(x.Key))
            .SelectMany(x => x.Value)
            .ToArray();

        var result = Filter();

        result.Length.Should().Be(events.Length);
        foreach (var row in events)
        {
            result.Should().Contain(row);
        }
    }

    private EventRow<TestEventData>[] Filter()
    {
        var filter = new DbQuery<TestEventData>(
                _period.Start,
                _period.End!.Value,
                null,
                TimeZoneInfo.Utc.Id,
                DbFilterOptimization.Default);
        return filter.ToArray(_events.SelectMany(x => x.Value));
    }

    public void ToBeEmpty()
    {
        Filter().Should().BeEmpty();
    }

    private void Add(string tag, EventRow<TestEventData> row)
    {
        if (!_events.ContainsKey(tag))
            _events[tag] = new List<EventRow<TestEventData>>();

        _events[tag].Add(row);
    }
}