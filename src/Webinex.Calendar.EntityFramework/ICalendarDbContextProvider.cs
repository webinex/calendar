using Microsoft.EntityFrameworkCore;

namespace Webinex.Calendar.EntityFramework;

public interface ICalendarDbContextProvider<TData>
{
    DbContext Value { get; }
}

internal class CalendarDbContextProvider<TData, TDbContext> : ICalendarDbContextProvider<TData>
    where TDbContext : DbContext
{
    public DbContext Value { get; }

    public CalendarDbContextProvider(TDbContext dbContext)
    {
        Value = dbContext;
    }
}