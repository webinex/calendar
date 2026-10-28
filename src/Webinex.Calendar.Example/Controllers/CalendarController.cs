using Microsoft.AspNetCore.Mvc;

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
    public async Task<Occurrence<EventData>[]> GetAllAsync(DateTimeOffset from, DateTimeOffset to)
    {
        return (await _calendar.OccurrencesAsync(from, to)).ToArray();
    }

    [HttpPost]
    public async Task<IActionResult> CreateEventAsync([FromBody] CreateEventRequestDto request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        if (request.IsRecurrentEvent())
            await _calendar.AddAsync(request.ToRecurrentEvent());

        if (request.IsOneTimeEvent())
            await _calendar.AddAsync(request.ToOneTimeEvent());
        
        await _dbContext.SaveChangesAsync();
        return Ok();
    }

    [HttpPut("time")]
    public async Task<IActionResult> EditEventTimeAsync([FromBody] EditEventTimeRequestDto request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var id = OccurrenceId.Recurrent(request.RecurrentEventId, request.EventStart).ToString();
        await _calendar.MoveAsync(id, Period.New(request.MoveToStart, request.MoveToEnd));
        await _dbContext.SaveChangesAsync();

        return Ok();
    }

    [HttpPut("cancel/appearance")]
    public async Task<IActionResult> CancelEventAsync([FromBody] CancelRecurrentEventAppearanceRequestDto request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var id = OccurrenceId.Recurrent(request.RecurrentEventId, request.EventStart).ToString();
        await _calendar.CancelOccurrenceAsync(id);
        await _dbContext.SaveChangesAsync();
        return Ok();
    }

    [HttpPut("cancel/one-time")]
    public async Task<IActionResult> CancelEventAsync([FromBody] CancelOneTimeEventRequestDto request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var @event = await _calendar.ByIdAsync<Event<EventData>>(request.Id);
        if (@event == null)
            return NotFound(request.Id);
        
        await _calendar.CancelOccurrenceAsync(OccurrenceId.OneTime(@event.Id).ToString());
        await _dbContext.SaveChangesAsync();
        return Ok();
    }

    [HttpPut("cancel/recurrent")]
    public async Task<IActionResult> CancelEventAsync([FromBody] CancelRecurrentEventRequestDto request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var id = OccurrenceId.Recurrent(request.RecurrentEventId, request.Since).ToString();
        await _calendar.CancelOccurrenceAsync(id, OccurenceUpdateBehavior.Group);
        await _dbContext.SaveChangesAsync();
        return Ok();
    }
}
