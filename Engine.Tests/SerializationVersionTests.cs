using System.Linq;
using Engine.Core.Serialization;
using Engine.Core.Utilities;
using Xunit;

namespace Engine.Tests;

public class SerializationVersionTests
{
    [Fact]
    public void FutureSceneVersionThrowsWithFilePath()
    {
        string dir = NewTempDir();
        try
        {
            string path = Write(dir, "Main.scene", SceneYaml(version: 99));

            var ex = Assert.ThrowsAny<InvalidOperationException>(() => SceneSerializer.LoadScene(path));

            Assert.Contains("Version", ex.Message);
            Assert.Contains("99", ex.Message);
            Assert.Contains(path, ex.Message);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void MissingSceneVersionStaysLoadable()
    {
        string dir = NewTempDir();
        try
        {
            string yaml = SceneYaml(version: null);
            string path = Write(dir, "Main.scene", yaml);

            var scene = SceneSerializer.LoadScene(path);

            Assert.Equal("Main", scene.SceneName);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void StaleSceneVersionWarnsAndLoads()
    {
        string dir = NewTempDir();
        try
        {
            string path = Write(dir, "Main.scene", SceneYaml(version: 0));
            var messages = new List<string>();
            Log.OnLogMessage += Capture;
            try
            {
                var scene = SceneSerializer.LoadScene(path);
                Assert.Equal("Main", scene.SceneName);
            }
            finally
            {
                Log.OnLogMessage -= Capture;
            }

            Assert.Contains(messages, m => m.Contains("Version") && m.Contains("0"));

            void Capture(LogSeverity severity, string message) => messages.Add(message);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void StrictSceneLoadRejectsMalformedId()
    {
        string dir = NewTempDir();
        try
        {
            string path = Write(dir, "Main.scene", SceneYaml(version: 1, id: "not-a-guid"));

            Assert.Throws<InvalidOperationException>(() => SceneSerializer.LoadScene(path, strict: true));

            var recovered = SceneSerializer.LoadScene(path);
            Assert.NotEqual(Guid.Empty, recovered.Id);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void StrictSceneLoadRejectsUnknownManagerType()
    {
        string dir = NewTempDir();
        try
        {
            string yaml = SceneYaml(version: 1).Replace("Managers: []\n", "Managers:\n- Definitely.Not.A.Manager\n");
            string path = Write(dir, "Main.scene", yaml);

            Assert.Throws<InvalidOperationException>(() => SceneSerializer.LoadScene(path, strict: true));

            var skipped = SceneSerializer.LoadScene(path);
            Assert.Empty(skipped.Managers.GetRegisteredManagers());
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void StrictSceneLoadRejectsMalformedEntityId()
    {
        string dir = NewTempDir();
        try
        {
            string path = Write(dir, "Main.scene", SceneYaml(version: 1, includeEntities: false) + EntityYaml(id: "not-a-guid"));

            Assert.Throws<InvalidOperationException>(() => SceneSerializer.LoadScene(path, strict: true));

            var recovered = SceneSerializer.LoadScene(path);
            Assert.Single(recovered.Entities.GetSerializableEntities());
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void StrictSceneLoadRejectsUnknownComponentType()
    {
        string dir = NewTempDir();
        try
        {
            string entity = "Entities:\n- Name: Bad\n  Id: 22222222-2222-2222-2222-222222222222\n  ParentId: 00000000-0000-0000-0000-000000000000\n  Components:\n    Not.A.Component: {}\n";
            string path = Write(dir, "Main.scene", SceneYaml(version: 1, includeEntities: false) + entity);

            Assert.Throws<InvalidOperationException>(() => SceneSerializer.LoadScene(path, strict: true));

            var skipped = SceneSerializer.LoadScene(path);
            Assert.DoesNotContain(
                skipped.Entities.GetSerializableEntities()[0].Components,
                kvp => kvp.Key.Name == "Not.A.Component");
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void FutureMapVersionThrowsWithFilePath()
    {
        string dir = NewTempDir();
        try
        {
            string path = Write(dir, "Main_Map.yaml", "Version: 99\nMapName: Map\nWidth: 2\nHeight: 2\n");

            var ex = Assert.ThrowsAny<InvalidOperationException>(() => TileMapSerializer.LoadMap(path));

            Assert.Contains("Version", ex.Message);
            Assert.Contains(path, ex.Message);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void StrictDatabaseLoadRejectsMalformedId()
    {
        string dir = NewTempDir();
        try
        {
            string path = Write(dir, "tiledb.database", DatabaseYaml(id: "not-a-guid", dataType: nameof(FixtureTileDataComponent)));

            Assert.Throws<InvalidOperationException>(() => DatabaseSerializer.LoadDatabase(path, strict: true));

            var recovered = DatabaseSerializer.LoadDatabase(path);
            Assert.NotNull(recovered);
            Assert.NotEqual(Guid.Empty, recovered.ID);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void StrictDatabaseLoadRejectsUnknownDataType()
    {
        string dir = NewTempDir();
        try
        {
            string path = Write(dir, "tiledb.database", DatabaseYaml(id: Guid.NewGuid().ToString(), dataType: "Not.A.DataComponent"));

            Assert.Throws<InvalidOperationException>(() => DatabaseSerializer.LoadDatabase(path, strict: true));

            Assert.Null(DatabaseSerializer.LoadDatabase(path));
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    private static string NewTempDir()
    {
        string dir = Path.Combine(Path.GetTempPath(), "t2d-version-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        return dir;
    }

    private static string Write(string dir, string name, string yaml)
    {
        string path = Path.Combine(dir, name);
        File.WriteAllText(path, yaml);
        return path;
    }

    private static string SceneYaml(int? version, string id = "11111111-1111-1111-1111-111111111111", bool includeEntities = true)
    {
        string versionLine = version.HasValue ? $"Version: {version.Value}\n" : string.Empty;
        string entitiesLine = includeEntities ? "Entities: []\n" : string.Empty;
        return versionLine + $"SceneName: Main\nId: {id}\nTileMapFiles: []\nActiveMapIndex: 0\nManagers: []\nSystems: []\n" + entitiesLine;
    }

    private static string EntityYaml(string id)
    {
        return $"Entities:\n- Name: Bad\n  Id: {id}\n  ParentId: 00000000-0000-0000-0000-000000000000\n  Components: {{}}\n";
    }

    private static string DatabaseYaml(string id, string dataType)
    {
        return $"Version: 1\nID: {id}\nDataType: {dataType}\nName: tiledb\nDataEntries: {{}}\n";
    }
}
