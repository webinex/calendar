using Microsoft.AspNetCore.Mvc;
using Webinex.Calendar.Events;
using Webinex.Calendar.Repeats;

namespace Webinex.Calendar.Example.Controllers;

[ApiController]
[Route("/api/calendar")]
public class CalendarController : ControllerBase
{
    private readonly ICalendar<EventData> _calendar;
    private readonly ExampleDbContext _dbContext;

    public CalendarController(ICalendar<EventData> calendar, ExampleDbContext dbContext)
    {
        _calendar = calendar;
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<Event<EventData>[]> GetAllAsync(DateTimeOffset from, DateTimeOffset to)
    {
        return await _calendar.GetAllAsync(from, to);
    }

    [HttpPost]
    public async Task<IActionResult> CreateEventAsync([FromBody] CreateEventRequest request)
    {
        if (request.OneTime != null)
        {
            var @event = OneTimeEvent<EventData>.New(request.OneTime.Start, request.OneTime.End, request.OneTime.Data);
            await _calendar.AddOneTimeEventAsync(@event);
        }
        else if (request.Interval != null)
        {
            var @event = RecurrentEvent<EventData>.NewInterval(request.Interval.Start, null, request.Interval.IntervalMinutes,
                request.Interval.DurationMinutes, request.Interval.Data);
            await _calendar.AddRecurrentEventAsync(@event);
        }
        else if (request.Match != null)
        {
            var @event =
                RecurrentEvent<EventData>.NewMatch(request.Match.TimeOfTheDayUtcMinutes, request.Match.DurationMinutes,
                    request.Match.Weekdays.Select(x => new Weekday(x)).ToArray(),
                    request.Match.DayOfMonth != null ? new DayOfMonth(request.Match.DayOfMonth.Value) : null,
                    request.Match.Start,
                    request.Match.End,
                    request.Match.Data);

            await _calendar.AddRecurrentEventAsync(@event);
        }
        else
        {
            return BadRequest();
        }


        await _dbContext.SaveChangesAsync();
        return Ok();
    }

    public class CreateEventRequest
    {
        public CreateOneTimeEventPayload? OneTime { get; set; }
        public CreateMatchEventPayload? Match { get; set; }
        public CreateIntervalEventPayload? Interval { get; set; }
    }

    public class CreateOneTimeEventPayload
    {
        public DateTimeOffset Start { get; set; }
        public DateTimeOffset End { get; set; }
        public EventData Data { get; set; } = null!;
    }

    public class CreateMatchEventPayload
    {
        public DateTimeOffset Start { get; set; }
        public DateTimeOffset? End { get; set; }
        public int TimeOfTheDayUtcMinutes { get; set; }
        public int DurationMinutes { get; set; }
        public string[] Weekdays { get; set; } = null!;
        public int? DayOfMonth { get; set; }
        public EventData Data { get; set; } = null!;
    }

    public class CreateIntervalEventPayload
    {
        public DateTimeOffset Start { get; set; }
        public DateTimeOffset? End { get; set; }
        public int DurationMinutes { get; set; }
        public int IntervalMinutes { get; set; }
        public EventData Data { get; set; } = null!;
    }
}