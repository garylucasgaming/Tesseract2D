using Engine.Core.ECS;
using Engine.Core.Runtime;
using Xunit;

namespace Engine.Tests;

public sealed class RuntimeEntityFlagTests
{
    [Fact]
    public void CleanupRemovesOnlyExplicitlyRuntimeCreatedEntities()
    {
        var context = new EngineContext { PlayState = true };
        var scene = new GameScene(context);
        var persisted = scene.Spawn("Persisted");
        var runtime = scene.Spawn("Runtime", isRuntimeCreated: true);

        Assert.False(persisted.IsRuntimeCreated);
        Assert.True(runtime.IsRuntimeCreated);

        scene.CleanupRuntimeEntities();

        var remaining = scene.Entities.GetSerializableEntities();
        Assert.Contains(persisted, remaining);
        Assert.DoesNotContain(runtime, remaining);
    }
}
