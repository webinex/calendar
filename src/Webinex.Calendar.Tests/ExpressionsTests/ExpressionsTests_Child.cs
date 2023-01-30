using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using Webinex.Calendar.Filters;

namespace Webinex.Calendar.Tests.ExpressionsTests;

// ReSharper disable once InconsistentNaming
public class ExpressionsTests_Child
{
    private Entity[] _data = null!;

    [Test]
    public void WhenOneLevelDepth_ShouldBeOk()
    {
        var expression = Expressions.Child<Entity, Inner>(
            x => x.Child!,
            x => x.Name == "1-1");

        var result = _data.Where(expression.Compile()).ToArray();
        result.Length.Should().Be(1);

        result.Single().Name.Should().Be("1");
    }

    [Test]
    public void WhenTwoLevelDepth_ShouldBeOk()
    {
        var expression = Expressions.Child<Entity, Inner>(
            x => x.Child!,
            x => x.Child != null && x.Child.Name == "1-1-1");

        var result = _data.Where(expression.Compile()).ToArray();
        result.Length.Should().Be(1);

        result.Single().Name.Should().Be("1");
    }

    [SetUp]
    public void SetUp()
    {
        _data = new[]
        {
            new Entity { Name = "1", Child = new Inner { Name = "1-1", Child = new Inner { Name = "1-1-1" } } },
            new Entity { Name = "2", Child = new Inner { Name = "2-1" } },
        };
    }

    private class Entity
    {
        public string Name { get; init; } = null!;
        public Inner? Child { get; init; }
    }
    
    private class Inner
    {
        public string Name { get; init; } = null!;
        public Inner? Child { get; init; }
    }
}