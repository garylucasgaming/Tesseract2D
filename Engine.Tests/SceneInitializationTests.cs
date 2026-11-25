using System.Reflection;
using Engine.Core.ECS;
using Engine.Core.Serialization;
using Xunit;

namespace Engine.Tests;

public class SceneInitializationTests
{
    [Fact]
    public void RepeatedManagerInitializationKeepsOneEventSubscription()
    {
        var scene = new GameScene();
        var entities = scene.Entities;

        Assert.Equal(1, EntityCreatedSubscribers(scene));

        scene.InitializeManagers();
        scene.resetContextSceneInManagers();

        Assert.Same(entities, scene.Entities);
        Assert.Equal(1, EntityCreatedSubscribers(scene));
    }

    [Fact]
    public void BuiltInSystemsHaveSceneContextAfterConstruction()
    {
        var scene = new GameScene();

        Assert.NotEmpty(scene.Systems._systemEntityCache.Keys);
        Assert.All(scene.Systems._systemEntityCache.Keys, system => Assert.Same(scene, system.ContextScene));
    }

    [Fact]
    public void LoadedSceneHasWarmCachesAndSceneContexts()
    {
        string path = FixturePaths.Get("Content/Assets/Scenes/Main.scene");

        var loaded = SceneSerializer.LoadScene(path);

        Assert.NotEmpty(loaded.Systems._systemEntityCache.Keys);
        Assert.All(loaded.Systems._systemEntityCache.Keys, system => Assert.Same(loaded, system.ContextScene));

        var entities = loaded.Entities.GetSerializableEntities();
        Assert.NotEmpty(entities);
        Assert.Contains(entities[0], loaded.Systems._systemEntityCache[loaded.Systems.transformSystem]);
        Assert.Equal(1, EntityCreatedSubscribers(loaded));
    }

    private static int EntityCreatedSubscribers(GameScene scene)
    {
        var field = typeof(Engine.Core.ECS.Managers.EntityManager)
            .GetField("OnEntityCreated", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field);

        var handler = field.GetValue(scene.Entities) as Delegate;
        return handler?.GetInvocationList().Length ?? 0;
    }
}
