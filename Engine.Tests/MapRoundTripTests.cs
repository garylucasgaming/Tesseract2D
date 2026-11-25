using Engine.Core.Collections;
using Engine.Core.ECS;
using Engine.Core.GamePlay;
using Engine.Core.Serialization;
using Xunit;

namespace Engine.Tests;

public class MapRoundTripTests
{
    [Fact]
    public void SaveMapWritesTileIndexDataAndLoadRebindsByAssetId()
    {
        string dir = Path.Combine(Path.GetTempPath(), "t2d-map-roundtrip-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            Guid assetId = Guid.NewGuid();
            var component = new FixtureTileDataComponent
            {
                AssetID = assetId,
                DisplayName = "Stone Tile"
            };
            var database = new Database
            {
                Name = "tiledb",
                DatabaseType = typeof(FixtureTileDataComponent).Name
            };
            database.ComponentDatabase[assetId] = component;

            var scene = new GameScene();
            scene.Database.Databases.Add(database);

            var map = new Map(4, 4)
            {
                MapName = "RoundTrip",
                TileDatabaseName = "tiledb",
                ContextScene = scene
            };
            map.TileIndexDataDictionary[7] = component;

            string path = Path.Combine(dir, "RoundTrip.yaml");
            TileMapSerializer.SaveMap(map, path);

            string yaml = File.ReadAllText(path);
            Assert.Contains("TileIndexDataDictionary:", yaml);
            Assert.Contains(assetId.ToString(), yaml);

            var loaded = TileMapSerializer.LoadMap(path, scene);

            Assert.NotNull(loaded);
            Assert.True(loaded.TileIndexDataDictionary.TryGetValue(7, out var bound));
            Assert.Equal(assetId, bound.AssetID);
            Assert.Equal("Stone Tile", bound.DisplayName);
            Assert.Same(component, bound);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void StrictLoadMapThrowsWhenTileRowCannotBind()
    {
        string dir = Path.Combine(Path.GetTempPath(), "t2d-map-strict-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            var component = new FixtureTileDataComponent
            {
                AssetID = Guid.NewGuid(),
                DisplayName = "Unbound Tile"
            };
            var database = new Database
            {
                Name = "tiledb",
                DatabaseType = typeof(FixtureTileDataComponent).Name
            };
            database.ComponentDatabase[component.AssetID] = component;

            var scene = new GameScene();
            scene.Database.Databases.Add(database);
            var map = new Map(4, 4)
            {
                MapName = "Strict",
                TileDatabaseName = "tiledb",
                ContextScene = scene
            };
            map.TileIndexDataDictionary[7] = component;

            string path = Path.Combine(dir, "Strict.yaml");
            TileMapSerializer.SaveMap(map, path);

            var sceneWithoutDatabase = new GameScene();
            Assert.Throws<InvalidOperationException>(
                () => TileMapSerializer.LoadMap(path, sceneWithoutDatabase, strict: true));
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void LoadMapRebuildsPerTileDataFromFinalGrid()
    {
        string dir = Path.Combine(Path.GetTempPath(), "t2d-map-grid-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            Guid assetId = Guid.NewGuid();
            var component = new FixtureTileDataComponent
            {
                AssetID = assetId,
                DisplayName = "Stone Tile"
            };
            var database = new Database
            {
                Name = "tiledb",
                DatabaseType = typeof(FixtureTileDataComponent).Name
            };
            database.ComponentDatabase[assetId] = component;

            var scene = new GameScene();
            scene.Database.Databases.Add(database);
            var map = new Map(4, 4)
            {
                MapName = "Grid",
                TileDatabaseName = "tiledb",
                ContextScene = scene,
                TileProperties = new Dictionary<int, int> { [7] = 2 }
            };
            map.TileIndexDataDictionary[7] = component;
            map.GridFlattened = Enumerable.Range(0, 16).Select(i => i == 5 ? 2 : 0).ToList();

            string path = Path.Combine(dir, "Grid.yaml");
            TileMapSerializer.SaveMap(map, path);

            var loaded = TileMapSerializer.LoadMap(path, scene);
            Assert.NotNull(loaded);

            var tileData = loaded.GetTileData(1, 1);
            Assert.NotNull(tileData);
            Assert.Equal(assetId, tileData.AssetID);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }
}
