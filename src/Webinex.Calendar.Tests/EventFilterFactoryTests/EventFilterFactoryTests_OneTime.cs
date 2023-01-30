using Webinex.Calendar.DataAccess;

namespace Webinex.Calendar.Tests.EventFilterFactoryTests;

// ReSharper disable once InconsistentNaming
public class EventFilterFactoryTests_OneTime : EventFilterFactoryTests_ExactDateEventBase
{
    protected override EventRowType Type => EventRowType.OneTimeEvent;
}