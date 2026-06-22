using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Webinex.Calendar.EntityFramework;

public static class EventRowModelBuilderExtensions
{
    public static ModelBuilder AddEvent<TData>(
        this ModelBuilder model,
        string schemaName = "dbo",
        string tableName = "Events",
        Action<OwnedNavigationBuilder<EventRow<TData>, TData>>? configureData = null,
        Action<EntityTypeBuilder<EventRow<TData>>>? postConfigure = null)
        where TData : class, ICloneable
    {
        model.Entity<EventRow<TData>>(entity =>
        {
            entity.ToTable(tableName, schemaName);
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Type).IsRequired();

            entity.OwnsOne(x => x.Period, period =>
            {
                period.Property(x => x.Start).IsRequired().HasPrecision(0);
                period.Property(x => x.End).IsRequired().HasPrecision(0);
            });

            entity.Property(x => x.TimeZone).HasMaxLength(50);

            entity.OwnsOne(x => x.Effective, effective =>
            {
                effective.Property(x => x.Start).IsRequired().HasPrecision(0);
                effective.Property(x => x.End).HasPrecision(0);
            });

            entity.OwnsOne(x => x.Group, group =>
            {
                group.Property(x => x.Id).IsRequired().HasMaxLength(250);
                group.Property(x => x.Offset).IsRequired().HasPrecision(0);
            });
            
            if (typeof(TData) == typeof(object))
                entity.Ignore(x => x.Data);
            else if (configureData != null)
                entity.OwnsOne(x => x.Data, configureData);
            else
                entity.OwnsOne(x => x.Data);

            entity.OwnsOne(x => x.MoveTo, moveTo =>
            {
                moveTo.Property(x => x.Start).IsRequired().HasPrecision(0);
                moveTo.Property(x => x.End).IsRequired().HasPrecision(0);
            });
            
            postConfigure?.Invoke(entity);
        });
        
        return model;
    }
}