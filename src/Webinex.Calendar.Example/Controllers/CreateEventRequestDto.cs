using System.ComponentModel.DataAnnotations;
using Webinex.Calendar.Common;
using Webinex.Calendar.Events;

namespace Webinex.Calendar.Example.Controllers;

public enum CreateEventRequestType
{
    OneTime, RepeatWeekday, RepeatDayOfMonth, RepeatInterval,
}

public class CreateEventRequestDto : IValidatableObject
{
    public CreateEventRequestType Type { get; init; }
    public string Title { get; init; } = null!;
    public DateTimeOffset Start { get; init; }
    public DateTimeOffset? End { get; init; }
    public string[]? Weekdays { get; init; }
    public int? DayOfMonth { get; init; }
    public int? TimeOfTheDayUtcMinutes { get; init; }
    public int? DurationMinutes { get; init; }
    public int? IntervalMinutes { get; init; }

    public bool IsRecurrentEvent() => Type is CreateEventRequestType.RepeatInterval or CreateEventRequestType.RepeatWeekday or CreateEventRequestType.RepeatDayOfMonth;

    public bool IsOneTimeEvent() => Type == CreateEventRequestType.OneTime;

    public OneTimeEvent<EventData> ToOneTimeEvent()
    {
        if (!IsOneTimeEvent())
            throw new InvalidOperationException();
        
        return OneTimeEvent<EventData>.New(new Period(Start, End!.Value), new EventData(Title));
    }

    public RecurrentEvent<EventData> ToRecurrentEvent()
    {
        if (!IsRecurrentEvent())
            throw new InvalidOperationException();

        return Type switch
        {
            CreateEventRequestType.RepeatInterval => RecurrentEvent<EventData>.NewInterval(
                Start.AddMinutes(TimeOfTheDayUtcMinutes!.Value),
                End,
                IntervalMinutes!.Value,
                DurationMinutes!.Value,
                new EventData(Title)),

            CreateEventRequestType.RepeatWeekday => RecurrentEvent<EventData>.NewWeekday(
                Start,
                End,
                TimeOfTheDayUtcMinutes!.Value,
                DurationMinutes!.Value,
                Weekdays!.Select(x => new Weekday(x)).ToArray(),
                TimeZoneInfo.Utc,
                new EventData(Title)),

            CreateEventRequestType.RepeatDayOfMonth => RecurrentEvent<EventData>.NewDayOfMonth(
                Start,
                End,
                TimeOfTheDayUtcMinutes!.Value,
                DurationMinutes!.Value,
                new DayOfMonth(DayOfMonth!.Value),
                TimeZoneInfo.Utc,
                new EventData(Title)),

            _ => throw new ArgumentOutOfRangeException(),
        };
    }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Type == CreateEventRequestType.OneTime)
        {
            if (!End.HasValue)
                return Failed("End required");
        }

        if (Type == CreateEventRequestType.RepeatWeekday)
        {
            if (Weekdays?.Any() != true)
                return Failed("At least one weekday required");
        }

        if (Type == CreateEventRequestType.RepeatDayOfMonth)
        {
            if (!DayOfMonth.HasValue)
                return Failed("DayOfMonth might not be null");
        }

        if (Type == CreateEventRequestType.RepeatInterval)
        {
            if (!IntervalMinutes.HasValue)
                return Failed("IntervalMinutes might not be null");
        }

        if (IsRecurrentEvent())
        {
            if (!TimeOfTheDayUtcMinutes.HasValue)
                return Failed("TimeOfTheDayUtcMinutes might not be null");
            if (!DurationMinutes.HasValue)
                return Failed("DurationMinutes might not be null");
        }

        return Array.Empty<ValidationResult>();
    }

    private IEnumerable<ValidationResult> Failed(string message)
    {
        return new[] { new ValidationResult(message) };
    }
}