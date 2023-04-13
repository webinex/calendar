using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Webinex.Calendar;
using Webinex.Calendar.Example;

var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    WebRootPath = "wwwroot/build",
});

// Add services to the container.

builder.Services.AddControllers()
    .AddJsonOptions(x => x.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCalendar<EventData>(x => x
    .AddDbContext<ExampleDbContext>());

builder.Services.AddDbContext<ExampleDbContext>(x =>
    x.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

if (!builder.Environment.IsDevelopment())
{
    app.UseDefaultFiles();
    app.UseStaticFiles();

    app.UseWhen(
        x => !x.Request.Path.StartsWithSegments("/api") && x.Request.Path != "/health",
        o => o.UseSpa(_ => { }));
}

app.UseAuthorization();

app.MapControllers();

app.Run();