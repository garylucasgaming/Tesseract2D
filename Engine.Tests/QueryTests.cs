using Engine.Core.ECS;
using Xunit;

namespace Engine.Tests;

public class QueryTests
{
    [Fact]
    public void QueryHasIsPolymorphicWhileHasComponentIsExact()
    {
        var entity = new GameObject { Name = "derived" };
        entity.AddComponent(new DerivedFixtureComponent());

        Assert.True(Query.Has<BaseFixtureComponent>().IsMatched(entity));
        Assert.True(entity.HasComponent<DerivedFixtureComponent>());
        Assert.False(entity.HasComponent<BaseFixtureComponent>());
    }

    [Fact]
    public void AndOrNotComposeWithoutGraphics()
    {
        var entity = new GameObject { Name = "derived" };
        entity.AddComponent(new DerivedFixtureComponent());

        Assert.True(Query.Has<DerivedFixtureComponent>()
            .And(Query.Not<OtherFixtureComponent>())
            .IsMatched(entity));
        Assert.False(Query.Not<BaseFixtureComponent>().IsMatched(entity));
        Assert.True(Query.Or(
            Query.Has<OtherFixtureComponent>(),
            Query.Has<DerivedFixtureComponent>()).IsMatched(entity));
        Assert.False(Query.And(
            Query.Has<DerivedFixtureComponent>(),
            Query.Has<OtherFixtureComponent>()).IsMatched(entity));
    }
}
