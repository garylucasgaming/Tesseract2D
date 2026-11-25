using Engine.Core.ECS;
using Engine.Core.Runtime;
using Engine.Core.Serialization;
using Microsoft.Xna.Framework;
using Xunit;

namespace Engine.Tests;

public class ComponentSerializerTests
{
    [Fact]
    public void SupportedValueKindsRoundTrip()
    {
        var source = new PrimitiveFixtureComponent
        {
            Count = 7,
            Label = "hello",
            Kind = FixtureKind.Beta,
            Position = new Vector2(12.5f, -3f)
        };

        var exported = ComponentSerializer.ExportComponent(source);

        Assert.Equal(7, Convert.ToInt32(exported["Count"]));
        Assert.Equal("hello", exported["Label"]);
        Assert.Equal("Beta", exported["Kind"]);
        Assert.IsType<Dictionary<string, float>>(exported["Position"]);
        Assert.False(exported.ContainsKey("Skipped"));

        var target = new PrimitiveFixtureComponent();
        ComponentSerializer.ImportComponent(target, exported);

        Assert.Equal(7, target.Count);
        Assert.Equal("hello", target.Label);
        Assert.Equal(FixtureKind.Beta, target.Kind);
        Assert.Equal(new Vector2(12.5f, -3f), target.Position);
    }

    [Fact]
    public void ReferencePayloadsResolveAgainstIdMap()
    {
        var first = new GameObject { Name = "First" };
        var second = new GameObject { Name = "Second" };
        var secondComponent = new PrimitiveFixtureComponent();
        second.AddComponent(secondComponent);

        var source = new ReferenceFixtureComponent
        {
            TargetObject = second,
            TargetComponent = secondComponent,
            Targets = new List<GameObject> { first, second },
            OnActivate = new GameEvent
            {
                TargetGameObject = second,
                TargetComponentTypeName = nameof(PrimitiveFixtureComponent),
                MethodName = "Ping"
            }
        };

        var exported = ComponentSerializer.ExportComponent(source);
        var target = new ReferenceFixtureComponent();
        var idMap = new Dictionary<Guid, GameObject>
        {
            [first.Id] = first,
            [second.Id] = second
        };

        ComponentSerializer.ImportComponent(target, exported);
        ComponentSerializer.ResolveComponentReferences(target, exported, idMap);

        Assert.Same(second, target.TargetObject);
        Assert.Same(secondComponent, target.TargetComponent);
        Assert.Equal(2, target.Targets.Count);
        Assert.Same(first, target.Targets[0]);
        Assert.Same(second, target.Targets[1]);
        Assert.NotNull(target.OnActivate);
        Assert.Same(second, target.OnActivate.TargetGameObject);
        Assert.Equal(nameof(PrimitiveFixtureComponent), target.OnActivate.TargetComponentTypeName);
        Assert.Equal("Ping", target.OnActivate.MethodName);
    }
}
