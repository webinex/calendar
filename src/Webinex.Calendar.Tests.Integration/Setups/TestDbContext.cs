using Microsoft.EntityFrameworkCore;
using Webinex.Calendar.EntityFramework;

namespace Webinex.Calendar.Tests.Integration.Setups;

public class TestDbContext : DbContext
{
    public TestDbContext()
        : base(new DbContextOptionsBuilder<TestDbContext>().UseSqlServer(SQL_DB_CONNECTION_STRING).Options)
    {
    }

    protected override void OnModelCreating(ModelBuilder model)
    {
        model.AddEvent<EventData>(
            schemaName: SCHEMA_NAME,
            tableName: EVENTS_TABLE_NAME,
            configureData: data =>
            {
                data.OwnsOne(
                    e => e.NValue,
                    n => n.Property(e => e.Value).HasColumnName("Data_NValue").HasMaxLength(250));
                data.Property(x => x.Name).HasColumnName("Data_Name").HasMaxLength(250);
            });

        model.AddRecurrentEvent<EventData>(
            schemaName: SCHEMA_NAME,
            tableName: RECURRENT_EVENTS_TABLE_NAME,
            configureData: data =>
            {
                data.OwnsOne(
                    e => e.NValue,
                    n => n.Property(e => e.Value).HasColumnName("Data_NValue").HasMaxLength(250));
                data.Property(x => x.Name).HasColumnName("Data_Name").HasMaxLength(250);
            });
    }
}