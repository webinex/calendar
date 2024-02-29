using Microsoft.EntityFrameworkCore;
using Webinex.Calendar.DataAccess;

namespace Webinex.Calendar.Tests.Integration.Setups;

public class TestDbContext : DbContext, ICalendarDbContext<EventData>
{
    public TestDbContext()
        : base(new DbContextOptionsBuilder<TestDbContext>().UseSqlServer(SQL_DB_CONNECTION_STRING).Options)
    {
    }

    public DbSet<EventRow<EventData>> Events { get; protected set; } = null!;

    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<EventRow<EventData>>(row =>
        {
            row.ToTable(EVENTS_TABLE_NAME, SCHEMA_NAME);
            row.HasKey(x => x.Id);

            row
                .HasOne(x => x.RecurrentEvent)
                .WithOne()
                .HasForeignKey<EventRow<EventData>>(x => x.RecurrentEventId)
                .OnDelete(DeleteBehavior.Restrict);

            row.OwnsOne(x => x.Effective, effective =>
            {
                effective.Property(x => x.Start).HasColumnName("Effective_Start");
                effective.Property(x => x.End).HasColumnName("Effective_End");
            });

            row.OwnsOne(x => x.MoveTo, moveTo =>
            {
                moveTo.Property(x => x.Start).HasColumnName("MoveTo_Start");
                moveTo.Property(x => x.End).HasColumnName("MoveTo_End");
            });

            row.OwnsOne(x => x.Repeat, repeat =>
            {
                repeat.Property(x => x.Type).HasColumnName("Repeat_Type");
                repeat.Property(x => x.Interval).HasColumnName("Repeat_Interval");
                repeat.Property(x => x.DurationMinutes).HasColumnName("Repeat_DurationMinutes");
                repeat.Property(x => x.TimeOfTheDayInMinutes).HasColumnName("Repeat_TimeOfTheDayInMinutes");
                repeat.Property(x => x.OvernightDurationMinutes).HasColumnName("Repeat_OvernightDurationMinutes");
                repeat.Property(x => x.SameDayLastTime).HasColumnName("Repeat_SameDayLastTime");
                repeat.Property(x => x.TimeZone).HasColumnName("Repeat_TimeZone");
                repeat.Property(x => x.Monday).HasColumnName("Repeat_Monday");
                repeat.Property(x => x.Tuesday).HasColumnName("Repeat_Tuesday");
                repeat.Property(x => x.Wednesday).HasColumnName("Repeat_Wednesday");
                repeat.Property(x => x.Thursday).HasColumnName("Repeat_Thursday");
                repeat.Property(x => x.Friday).HasColumnName("Repeat_Friday");
                repeat.Property(x => x.Saturday).HasColumnName("Repeat_Saturday");
                repeat.Property(x => x.Sunday).HasColumnName("Repeat_Sunday");
                repeat.Property(x => x.DayOfMonth).HasColumnName("Repeat_DayOfMonth");
            });

            row.OwnsOne(x => x.Data, o => { o.Property(x => x.Name).HasColumnName("Data_Name").HasMaxLength(250); });
        });
    }
}