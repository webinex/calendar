using Microsoft.EntityFrameworkCore;

namespace Webinex.Calendar.EntityFramework;

internal static class DbContextExtensions
{
    public static async Task<IReadOnlyCollection<T>> FindManyAsync<T>(this DbSet<T> dbSet, IEnumerable<string> ids)
        where T : class, IEventRow
    {
        ids = ids.Distinct().ToArray();
        if (!ids.Any())
            return [];

        var local = dbSet.Local.Where(x => ids.Contains(x.Id)).ToArray();
        var idSurplus = ids.Except(local.Select(x => x.Id)).ToArray();
        if (idSurplus.Length == 0)
            return local;

        var db = await dbSet.Where(x => ((IEnumerable<string>)idSurplus).Contains(x.Id)).ToArrayAsync();
        return local.Concat(db).ToArray();
    }
}