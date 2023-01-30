﻿using Microsoft.EntityFrameworkCore;
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

                repeat.OwnsOne(x => x.Interval, interval =>
                {
                    interval.Property(x => x.IntervalMinutes).HasColumnName("Repeat_Interval_IntervalMinutes");
                    interval.Property(x => x.StartSince1990Minutes)
                        .HasColumnName("Repeat_Interval_StartSince1990Minutes");
                    interval.Property(x => x.EndSince1990Minutes)
                        .HasColumnName("Repeat_Interval_EndSince1990Minutes");
                });

                repeat.OwnsOne(x => x.Match, match =>
                {
                    match.Property(x => x.DurationMinutes).HasColumnName("Repeat_Match_DurationMinutes");
                    match.Property(x => x.OvernightDurationMinutes)
                        .HasColumnName("Repeat_Match_OvernightDurationMinutes");
                    match.Property(x => x.SameDayLastTime).HasColumnName("Repeat_Match_SameDayLastTime");
                    match.Property(x => x.TimeOfTheDayUtcMinutes).HasColumnName("Repeat_Match_TimeOfTheDayUtcMinutes");
                    match.Property(x => x.DayOfMonth).HasColumnName("Repeat_Match_DayOfMonth");

                    match.Property(x => x.Monday).HasColumnName("Repeat_Match_Monday");
                    match.Property(x => x.Tuesday).HasColumnName("Repeat_Match_Tuesday");
                    match.Property(x => x.Wednesday).HasColumnName("Repeat_Match_Wednesday");
                    match.Property(x => x.Thursday).HasColumnName("Repeat_Match_Thursday");
                    match.Property(x => x.Friday).HasColumnName("Repeat_Match_Friday");
                    match.Property(x => x.Saturday).HasColumnName("Repeat_Match_Saturday");
                    match.Property(x => x.Sunday).HasColumnName("Repeat_Match_Sunday");
                });
            });

            row.OwnsOne(x => x.Data, o => { o.Property(x => x.Name).HasColumnName("Data_Name").HasMaxLength(250); });
        });
    }
}