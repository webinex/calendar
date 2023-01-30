using Webinex.Calendar.Repeats;

namespace Webinex.Calendar.DataAccess;

public class EventRowRepeat
{
    protected EventRowRepeat()
    {
    }
    
    public RepeatInterval? Interval { get; protected set; }
    public EventRowRepeatMatch? Match { get; protected set; }
    public EventRowRepeatType Type { get; protected set; }

    public static EventRowRepeat From(Repeat repeat)
    {
        return new EventRowRepeat
        {
            Interval = repeat.Interval,
            Match = repeat.Match != null ? EventRowRepeatMatch.From(repeat.Match) : null,
            Type = repeat.Match != null ? EventRowRepeatType.Match : EventRowRepeatType.Interval,
        };
    }

    public Repeat ToModel()
    {
        if (Type == EventRowRepeatType.Interval)
            return Repeat.NewInterval(Interval!);
        
        if (Type == EventRowRepeatType.Match)
            return Repeat.NewMatch(Match!.ToModel());

        throw new InvalidOperationException($"Unexpected {nameof(EventRowRepeat)} to have all null repeats");
    }
}