namespace Webinex.Calendar.EntityFramework;

public interface IEventRow : IEventEntityBase
{
    IEventEntityBase ToEventEntity();
    void Apply(IEventEntityBase value);
}