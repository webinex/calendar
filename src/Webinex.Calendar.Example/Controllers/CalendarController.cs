using Microsoft.AspNetCore.Mvc;
using Webinex.Calendar.Common;
using Webinex.Calendar.Events;

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
        return await _calendar.GetCalculatedAsync(from, to);
    }

    [HttpPost]
    public async Task<IActionResult> CreateEventAsync([FromBody] CreateEventRequestDto request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        if (request.IsRecurrentEvent())
            await _calendar.Recurrent.AddAsync(request.ToRecurrentEvent());

        if (request.IsOneTimeEvent())
            await _calendar.OneTime.AddAsync(request.ToOneTimeEvent());
        
        await _dbContext.SaveChangesAsync();
        return Ok();
    }

    [HttpPut("time")]
    public async Task<IActionResult> EditEventTimeAsync([FromBody] EditEventTimeRequestDto request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var recurrentEvent = await _calendar.Recurrent.GetAsync(request.RecurrentEventId);
        await _calendar.Recurrent.MoveAsync(recurrentEvent!, request.EventStart,
            new Period(request.MoveToStart, request.MoveToEnd));
        await _dbContext.SaveChangesAsync();

        return Ok();
    }

    [HttpPut("cancel/appearance")]
    public async Task<IActionResult> CancelEventAsync([FromBody] CancelRecurrentEventAppearanceRequestDto request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var recurrentEvent = await _calendar.Recurrent.GetAsync(request.RecurrentEventId);
        await _calendar.Recurrent.CancelAppearanceAsync(recurrentEvent!, request.EventStart);
        await _dbContext.SaveChangesAsync();
        return Ok();
    }

    [HttpPut("cancel/one-time")]
    public async Task<IActionResult> CancelEventAsync([FromBody] CancelOneTimeEventRequestDto request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var @event = await _calendar.OneTime.GetAsync(request.Id);
        if (@event == null)
            return NotFound(request.Id);
        
        await _calendar.OneTime.CancelAsync(@event);
        await _dbContext.SaveChangesAsync();
        return Ok();
    }

    [HttpPut("cancel/recurrent")]
    public async Task<IActionResult> CancelEventAsync([FromBody] CancelRecurrentEventRequestDto request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        await _calendar.Recurrent.CancelAsync(request.RecurrentEventId, request.Since);
        await _dbContext.SaveChangesAsync();
        return Ok();
    }
}