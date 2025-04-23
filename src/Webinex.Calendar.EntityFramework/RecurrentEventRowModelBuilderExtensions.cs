using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Webinex.Calendar.EntityFramework;

public static class RecurrentEventRowModelBuilderExtensions
{
    public static ModelBuilder AddRecurrentEvent<TData>(
        this ModelBuilder model,
        string schemaName = "dbo",
        string tableName = "RecurrentEvents",
        Action<OwnedNavigationBuilder<RecurrentEventRow<TData>, TData>>? configureData = null,
        Action<EntityTypeBuilder<RecurrentEventRow<TData>>>? postConfigure = null)
        where TData : class, ICloneable
    {
        model.Entity<RecurrentEventRow<TData>>(row =>
        {
            row.ToTable(tableName, schemaName);
            row.HasKey(x => x.Id);
            row.Property(x => x.Id).HasMaxLength(250).IsRequired();

            row.OwnsOne(x => x.Group, group =>
            {
                group.Property(x => x.Id).IsRequired();
                group.Property(x => x.Offset).HasPrecision(0).IsRequired();
            });

            row.OwnsOne(x => x.Effective, effective =>
            {
                effective.Property(x => x.Start).HasPrecision(0).IsRequired();
                effective.Property(x => x.End).HasPrecision(0);
            });

            row.OwnsOne(x => x.Period, period =>
            {
                period.Property(x => x.Start).HasPrecision(0).IsRequired();
                period.Property(x => x.End).HasPrecision(0).IsRequired();
            });

            row.Property(x => x.TimeZone).HasMaxLength(50).IsRequired();

            row.OwnsOne(x => x.Recurrence, recurrence =>
            {
                recurrence.ToJson();
                recurrence.OwnsOne(x => x.MGRecurrence, mgRecurrence =>
                {
                    mgRecurrence.OwnsOne(x => x.Period);
                    mgRecurrence.OwnsOne(x => x.Pattern, pattern =>
                    {
                        pattern.Property("_daysOfWeek").HasJsonPropertyName("DaysOfWeek");
                    });
                });
            });

            if (typeof(TData) == typeof(object))
                row.Ignore(x => x.Data);
            if (configureData == null)
                row.OwnsOne(x => x.Data);
            if (configureData != null)
                row.OwnsOne(x => x.Data, configureData);
            
            postConfigure?.Invoke(row);
        });

        return model;
    }
}