using Engine.Core.ECS;
using Engine.Core.ECS.Components;
using Engine.Core.Runtime;
using Engine.Core.Serialization;
using Xunit;

namespace Engine.Tests;

public class SceneSerializerResolverTests
{
    [Fact]
    public void LoadSceneResolvesManagerAndSystemAssemblyQualifiedNames()
    {
        string managerName = typeof(ResolverTestManager).AssemblyQualifiedName!;
        string systemName = typeof(ResolverTestSystem).AssemblyQualifiedName!;

        RunWithScene($"- '{managerName}'", $"- '{systemName}'", scene =>
        {
            Assert.NotNull(scene.Managers.GetManager<ResolverTestManager>());
            Assert.NotNull(scene.Systems.GetSystem<ResolverTestSystem>());
        });
    }

    [Fact]
    public void LoadSceneResolvesManagerAndSystemShortNames()
    {
        RunWithScene("- 'resolvertestmanager'", "- 'resolvertestsystem'", scene =>
        {
            Assert.NotNull(scene.Managers.GetManager<ResolverTestManager>());
            Assert.NotNull(scene.Systems.GetSystem<ResolverTestSystem>());
        });
    }

    [Fact]
    public void LoadSceneRejectsWrongBaseAndUnknownManagerAndSystemNames()
    {
        RunWithScene(
            $"- '{nameof(ResolverProbe)}'\n- 'Type.That.Does.Not.Exist'",
            $"- '{nameof(ResolverProbe)}'\n- 'Type.That.Does.Not.Exist'",
            scene =>
            {
                Assert.Empty(scene.Managers.GetRegisteredManagers());
                Assert.DoesNotContain(
                    scene.Systems._systemEntityCache.Keys,
                    system => system.GetType() == typeof(ResolverTestSystem));
            });
    }

    private static void RunWithScene(string managersYaml, string systemsYaml, Action<GameScene> assert)
    {
        string directory = Path.Combine(
            AppContext.BaseDirectory,
            "SceneResolverTests",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);

        string path = Path.Combine(directory, "Main.scene");
        try
        {
            File.WriteAllText(path, $$"""
                SceneName: Resolver
                Id: 11111111-1111-1111-1111-111111111111
                TileMapFiles: []
                ActiveMapIndex: 0
                Managers:
                {{managersYaml}}
                Systems:
                {{systemsYaml}}
                Entities: []
                """);

            assert(SceneSerializer.LoadScene(path));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    public sealed class ResolverTestManager : GameManager
    {
        public override void Initialize()
        {
        }

        public override void Update(float deltaTime)
        {
        }

        public override void Shutdown()
        {
        }
    }

    public sealed class ResolverTestSystem : GameSystem
    {
        public override IComponentQuery RequiredComponents
        {
            get;
            set;
        } = Query.Has<TransformComponent>();

        public override void Update(HashSet<GameObject> gameObjects, float deltaTime)
        {
        }
    }
}
