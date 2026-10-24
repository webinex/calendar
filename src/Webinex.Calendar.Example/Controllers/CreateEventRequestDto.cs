using System.ComponentModel.DataAnnotations;
using Webinex.Calendar.MicrosoftGraph;

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

    public Event<EventData> ToOneTimeEvent()
    {
        if (!IsOneTimeEvent())
            throw new InvalidOperationException();
        
        return Event.New(Period.New(Start, End!.Value), TimeZoneInfo.Utc.Id, new EventData(Title));
    }

    public Event<EventData> ToRecurrentEvent()
    {
        if (!IsRecurrentEvent())
            throw new InvalidOperationException();

        var start = new DateTimeOffset(Start.UtcDateTime.Date, TimeSpan.Zero)
            .AddMinutes(TimeOfTheDayUtcMinutes!.Value);
        // End is exclusive in the request; Graph recurrence uses an inclusive local date.
        var until = End.HasValue ? DateOnly.FromDateTime(End.Value.UtcDateTime.AddTicks(-1)) : (DateOnly?)null;
        var pattern = Type switch
        {
            CreateEventRequestType.RepeatInterval => MGRecurrencePattern.Daily(IntervalMinutes!.Value / (24 * 60)),
            CreateEventRequestType.RepeatWeekday => MGRecurrencePattern.Weekly(
                Weekdays!.Select(x => Enum.Parse<DayOfWeek>(x, ignoreCase: true))),
            CreateEventRequestType.RepeatDayOfMonth => MGRecurrencePattern.AbsoluteMonthly(DayOfMonth!.Value),
            _ => throw new ArgumentOutOfRangeException(),
        };

        return Event.New(
            Period.New(start, start.AddMinutes(DurationMinutes!.Value)),
            TimeZoneInfo.Utc.Id,
            new EventData(Title),
            new Recurrence(new MGRecurrence(new MGRecurrencePeriod(DateOnly.FromDateTime(start.UtcDateTime), until), pattern)));
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
            if (IntervalMinutes.Value < 24 * 60 || IntervalMinutes.Value % (24 * 60) != 0)
                return Failed("IntervalMinutes must be a positive whole number of days (multiples of 1440).");
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
