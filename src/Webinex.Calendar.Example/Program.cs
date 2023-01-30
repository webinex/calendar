using Microsoft.EntityFrameworkCore;
using Webinex.Calendar;
using Webinex.Calendar.DataAccess;
using Webinex.Calendar.Example;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCalendar<EventData>();

builder.Services.AddDbContext<ExampleDbContext>(x =>
    x.UseSqlServer("Server=localhost;Database=webinex_calendar;Trusted_Connection=True;TrustServerCertificate=True;"));

builder.Services.AddScoped<ICalendarDbContext<EventData>>(x => x.GetRequiredService<ExampleDbContext>());

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();

app.MapControllers();

app.Run();