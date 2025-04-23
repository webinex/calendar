namespace Webinex.Calendar;

public class CancelOccurrenceArgs
{
    public CancelOccurrenceArgs(string id, OccurenceUpdateBehavior behavior = OccurenceUpdateBehavior.Occurrence)
    {
        Id = id;
        Behavior = behavior;
    }

    public string Id { get; }
    public OccurenceUpdateBehavior Behavior { get; }
}