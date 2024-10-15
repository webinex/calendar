using System.Collections.Specialized;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Webinex.Calendar.DataAccess;

namespace Webinex.Calendar;

/// <summary>
/// Abstraction to use EF Core LocalView as Cache without unnecessary DetectChanges calls
/// </summary>
internal class EfLocalCache<TData> where TData : class, ICloneable
{
    private readonly HashSet<EventRow<TData>> _removed = new();

    private readonly LocalView<EventRow<TData>> _localView;

    public EfLocalCache(LocalView<EventRow<TData>> localView)
    {
        _localView = localView;
        _localView.CollectionChanged += OnCollectionChanged;
    }

    public IEnumerable<EventRow<TData>> AsEnumerable() => _localView;
    public IQueryable<EventRow<TData>> AsQueryable() => _localView.AsQueryable();

    // TODO s.sakharuk: With EF core 8+ we can use _localView.FindEntry() method to avoid DetectChanges
    public bool IsRemoved(EventRow<TData> row) => _removed.Contains(row);

    private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs args)
    {
        var addedItems = args.NewItems?.OfType<EventRow<TData>>()
                             .Except(args.OldItems?.OfType<EventRow<TData>>() ?? Array.Empty<EventRow<TData>>()) ??
                         Array.Empty<EventRow<TData>>();
        var removedItems = args.OldItems?.OfType<EventRow<TData>>()
                               .Except(args.NewItems?.OfType<EventRow<TData>>() ?? Array.Empty<EventRow<TData>>()) ??
                           Array.Empty<EventRow<TData>>();

        switch (args.Action)
        {
            case NotifyCollectionChangedAction.Remove:
            case NotifyCollectionChangedAction.Add:
            case NotifyCollectionChangedAction.Replace:
            {
                foreach (var row in removedItems)
                    _removed.Add(row);

                foreach (var row in addedItems)
                    _removed.Remove(row);
                break;
            }
            case NotifyCollectionChangedAction.Reset:
            {
                _removed.Clear();
                break;
            }
            case NotifyCollectionChangedAction.Move:
                break;
            default:
                throw new ArgumentOutOfRangeException($"Not supported {args.Action}");
        }
    }
}