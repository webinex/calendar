using Webinex.Calendar.DataAccess;

namespace Webinex.Calendar.Tests.EventFilterFactoryTests;

// ReSharper disable once InconsistentNaming
public class EventFilterFactoryTests_RecurrentEventData : EventFilterFactoryTests_ExactDateEventBase
{
    protected override EventRowType Type => EventRowType.RecurrentEventState;
}