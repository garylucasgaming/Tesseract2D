using Engine.Core.Collections;
using Engine.Core.ECS.Components;
using Engine.Core.Serialization;
using Xunit;

namespace Engine.Tests;

public class DatabaseSerializerTests
{
    [Fact]
    public void SaveThenLoadPreservesDatabaseIdentityAndTileRows()
    {
        var databaseId = Guid.Parse("aaaaaaaa-1111-2222-3333-444444444444");
        var assetId = Guid.Parse("bbbbbbbb-1111-2222-3333-444444444444");
        var database = new Database
        {
            ID = databaseId,
            Name = "RoundTripDb",
            DatabaseType = nameof(TileDataComponent)
        };
        var tile = new TileDataComponent
        {
            DisplayName = "Round Trip Tile",
            AssetID = assetId,
            TileSize = 32,
            SpritePath = "Assets/Textures/tile.png",
            TileType = TileType.Stone,
            hasCollider = true
        };
        database.ComponentDatabase[assetId] = tile;

        var path = Path.Combine(Path.GetTempPath(), $"t2d-db-roundtrip-{Guid.NewGuid():N}.database");

        try
        {
            DatabaseSerializer.SaveDatabase(database, path);
            var loaded = DatabaseSerializer.LoadDatabase(path);

            Assert.NotNull(loaded);
            Assert.Equal(databaseId, loaded.ID);
            Assert.Equal("RoundTripDb", loaded.Name);
            Assert.Equal(nameof(TileDataComponent), loaded.DatabaseType);
            Assert.Equal(assetId, Assert.Single(loaded.ComponentDatabase).Key);

            var loadedTile = Assert.IsType<TileDataComponent>(loaded.GetComponent(assetId));
            Assert.Equal("Round Trip Tile", loadedTile.DisplayName);
            Assert.Equal(32, loadedTile.TileSize);
            Assert.Equal("Assets/Textures/tile.png", loadedTile.SpritePath);
            Assert.Equal(TileType.Stone, loadedTile.TileType);
            Assert.True(loadedTile.hasCollider);
        }
        finally
        {
            if(File.Exists(path))
                File.Delete(path);
        }
    }
}
