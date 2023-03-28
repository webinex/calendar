using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Webinex.Calendar;
using Webinex.Calendar.Example;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers()
    .AddJsonOptions(x => x.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCalendar<EventData>(x => x
    .AddDbContext<ExampleDbContext>());

builder.Services.AddDbContext<ExampleDbContext>(x =>
    x.UseSqlServer("Server=localhost;Database=webinex_calendar;Trusted_Connection=True;TrustServerCertificate=True;"));

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