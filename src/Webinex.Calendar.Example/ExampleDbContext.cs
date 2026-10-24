using Microsoft.EntityFrameworkCore;
using Webinex.Calendar.EntityFramework;

namespace Webinex.Calendar.Example;

public class ExampleDbContext : DbContext
{
    private readonly IConfiguration _configuration;
    
    public ExampleDbContext(DbContextOptions<ExampleDbContext> options, IConfiguration configuration)
        : base(options)
    {
        _configuration = configuration;
    }

    public DbSet<EventRow<EventData>> Events { get; protected set; } = null!;

    protected override void OnModelCreating(ModelBuilder model)
    {
        model.AddEvent<EventData>(
            schemaName: _configuration["Db:SchemaName"]!,
            tableName: _configuration["Db:EventsTableName"]!,
            configureData: data => data.Property(x => x.Title).HasColumnName("Data_Title").HasMaxLength(250));

        model.AddRecurrentEvent<EventData>(
            schemaName: _configuration["Db:SchemaName"]!,
            tableName: _configuration["Db:RecurrentEventsTableName"] ?? "RecurrentEvents",
            configureData: data => data.Property(x => x.Title).HasColumnName("Data_Title").HasMaxLength(250));
    }
}
