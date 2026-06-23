using System;

namespace Webinex.Calendar.Tests;

public class None : ICloneable
{
    public object Clone()
    {
        return new None();
    }
}