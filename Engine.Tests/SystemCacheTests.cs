using Engine.Core.ECS;
using Xunit;

namespace Engine.Tests;

public class SystemCacheTests
{
    [Fact]
    public void RemovingARequiredComponentEvictsEntityFromSystemCache()
    {
        var scene = new GameScene();
        var probe = new CacheProbeSystem();
        scene.Systems.AddSystem(probe);

        var entity = new GameObject { Name = "Probe" };
        entity.AddComponent<CacheProbeComponent>();
        scene.AddGameObject(entity);

        Assert.Contains(entity, scene.Systems._systemEntityCache[probe]);

        scene.Entities.RemoveComponentFromEntity<CacheProbeComponent>(entity);

        Assert.DoesNotContain(entity, scene.Systems._systemEntityCache[probe]);
    }

    [Fact]
    public void DeactivatingAnEntityEvictsItAndReactivatingRestoresIt()
    {
        var scene = new GameScene();
        var probe = new CacheProbeSystem();
        scene.Systems.AddSystem(probe);

        var entity = new GameObject { Name = "Probe" };
        entity.AddComponent<CacheProbeComponent>();
        scene.AddGameObject(entity);

        Assert.Contains(entity, scene.Systems._systemEntityCache[probe]);

        entity.isActive = false;

        Assert.DoesNotContain(entity, scene.Systems._systemEntityCache[probe]);

        entity.isActive = true;

        Assert.Contains(entity, scene.Systems._systemEntityCache[probe]);
    }

    [Fact]
    public void RemovingAnEntityEvictsItFromSystemCache()
    {
        var scene = new GameScene();
        var probe = new CacheProbeSystem();
        scene.Systems.AddSystem(probe);

        var entity = new GameObject { Name = "Probe" };
        entity.AddComponent<CacheProbeComponent>();
        scene.AddGameObject(entity);

        Assert.Contains(entity, scene.Systems._systemEntityCache[probe]);

        scene.Entities.RemoveEntity(entity);

        Assert.DoesNotContain(entity, scene.Systems._systemEntityCache[probe]);
    }

    [Fact]
    public void RemovingDerivedComponentThroughBaseEvictsEntityFromSystemCache()
    {
        var scene = new GameScene();
        var probe = new CacheProbeSystem();
        scene.Systems.AddSystem(probe);

        var entity = new GameObject { Name = "Probe" };
        entity.AddComponent<DerivedCacheProbeComponent>();
        scene.AddGameObject(entity);

        Assert.Contains(entity, scene.Systems._systemEntityCache[probe]);

        scene.Entities.RemoveComponentFromEntity<CacheProbeComponent>(entity);

        Assert.DoesNotContain(entity, scene.Systems._systemEntityCache[probe]);
        Assert.DoesNotContain(
            entity.Components.Values,
            component => component is CacheProbeComponent);
    }

    [Fact]
    public void EntityUpdateSystemRunsAgainAfterComponentRematch()
    {
        var scene = new GameScene();
        var probe = new CacheProbeSystem
        {
            UpdatePolicy = SystemUpdatePolicy.EntityUpdate
        };
        scene.Systems.AddSystem(probe);

        var entity = new GameObject { Name = "Probe" };
        entity.AddComponent<CacheProbeComponent>();
        scene.AddGameObject(entity);

        Assert.Equal(1, probe.UpdateCount);

        scene.Entities.RemoveComponentFromEntity<CacheProbeComponent>(entity);
        entity.AddComponent<CacheProbeComponent>();

        Assert.Equal(2, probe.UpdateCount);
        Assert.Contains(entity, scene.Systems._systemEntityCache[probe]);
    }

    private class CacheProbeComponent : GameComponent
    {
    }

    private sealed class DerivedCacheProbeComponent : CacheProbeComponent
    {
    }

    private sealed class CacheProbeSystem : GameSystem
    {
        public override IComponentQuery RequiredComponents
        {
            get;
            set;
        } = Query.Has<CacheProbeComponent>();

        public int UpdateCount
        {
            get; private set;
        }

        public override void Update(HashSet<GameObject> gameObjects, float deltaTime)
        {
            UpdateCount++;
        }
    }
}
