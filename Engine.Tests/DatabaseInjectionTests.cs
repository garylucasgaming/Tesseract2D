using Engine.Core.Collections;
using Engine.Core.ECS;
using Engine.Core.GamePlay;
using Engine.Core.Runtime;
using Engine.Core.Serialization;
using Xunit;

namespace Engine.Tests;

public sealed class DatabaseInjectionTests
{
    [Fact]
    public void SceneUsesInjectedDatabaseManagerWithoutEditorProjectState()
    {
        var fixtureRoot = Path.Combine(AppContext.BaseDirectory, "TestContent", "Fonts");
        var context = new EngineContext { CurrentProjectRoot = fixtureRoot };
        var database = new DatabaseManager();
        database.LoadAllDatabasesFromFolder(context.DatabasePath);

        var scene = SceneSerializer.LoadScene(
            Path.Combine(fixtureRoot, "Content", "Assets", "Scenes", "Main.scene"),
            context: context,
            database: database);

        Assert.Same(database, scene.Database);
        Assert.Single(scene.Database.Databases);
        Assert.Equal("tiledb", scene.Database.Databases[0].Name);

        var map = Assert.Single(scene.SceneMaps);
        Assert.NotNull(map.TileDatabase);
        Assert.Same(scene.Database.Databases[0], map.TileDatabase);
    }

    [Fact]
    public void MapDoesNotResolveDatabaseFromGlobalActiveScene()
    {
        var previousActiveScene = EngineContext.Current.ActiveScene;
        var decoyDatabase = new Database { Name = "tiledb" };
        var decoyScene = new GameScene(
            new EngineContext(),
            new DatabaseManager { Databases = new List<Database> { decoyDatabase } });
        var map = new Map(1, 1) { TileDatabaseName = "tiledb" };

        try
        {
            EngineContext.Current.ActiveScene = decoyScene;

            map.ResolveDatabase();

            Assert.Null(map.TileDatabase);
        }
        finally
        {
            EngineContext.Current.ActiveScene = previousActiveScene;
        }
    }
}
