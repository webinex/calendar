using Webinex.Calendar.DataAccess;
using Webinex.Calendar.Events;

namespace Webinex.Calendar.Tests.EventFilterFactoryTests;

// ReSharper disable once InconsistentNaming
public class EventFilterFactoryTests_OneTime : EventFilterFactoryTests_ExactDateEventBase
{
    protected override EventType Type => EventType.OneTimeEvent;
}