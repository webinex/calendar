using System.Collections.Concurrent;

namespace Webinex.Calendar.EntityFramework;

internal class EventIdentityMap
{
    private readonly ConcurrentDictionary<string, IEventEntityBase> _map = new();

    public IEventEntityBase GetOrAdd(IEventRow row)
    {
        if (_map.TryGetValue(row.Id, out var result))
            return result;

        result = row.ToEventEntity();
        return Add(result);
    }

    public IEventEntityBase Add(IEventRow row, IEventEntityBase entity)
    {
        if (!TryAdd(row, entity))
            throw new InvalidOperationException($"Entity with id {row.Id} already exists in the map");
        
        return entity;
    }

    public bool TryAdd(IEventRow row, IEventEntityBase entity)
    {
        if (row.Id != entity.Id)
            throw new InvalidOperationException($"Row id  {row.Id} and entity id {entity.Id} doesn't match");

        return _map.TryAdd(row.Id, entity);
    }

    public bool TryRemove(IEventEntityBase entity)
    {
        return _map.TryRemove(entity.Id, out _);
    }

    private IEventEntityBase Add(IEventEntityBase entity)
    {
        return _map[entity.Id] = entity;
    }
}