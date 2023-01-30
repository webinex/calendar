using Microsoft.EntityFrameworkCore;

namespace Webinex.Calendar.DataAccess;

public interface ICalendarDbContext<TData>
    where TData : class, ICloneable
{
    DbSet<EventRow<TData>> Events { get; }
}