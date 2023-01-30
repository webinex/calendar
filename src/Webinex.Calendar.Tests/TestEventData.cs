using System;

namespace Webinex.Calendar.Tests;

public class TestEventData : ICloneable
{
    public object Clone()
    {
        return new TestEventData();
    }
}