using Engine.Core.ECS.Components;
using Engine.Core.Serialization;
using Xunit;

namespace Engine.Tests;

public class FixtureDatabaseTests
{
    private static readonly Guid WaterId = Guid.Parse("cdbd33eb-bd9f-4091-826c-5bde028fb442");
    private static readonly Guid StoneId = Guid.Parse("3c200462-c7dd-4522-bba3-baf6bceef243");
    private static readonly Guid GrassId = Guid.Parse("e62fef10-8292-4f35-be5a-0016d405ad9b");
    private static readonly Guid AirId = Guid.Parse("79378f0e-3c95-4c49-8f3e-695b32ec4c77");

    [Fact]
    public void PopulatedFontsDatabaseLoadsWithStableAssetIds()
    {
        var database = DatabaseSerializer.LoadDatabase(
            FixturePaths.Get("Content/Assets/Databases/tiledb.database"));

        Assert.NotNull(database);
        Assert.Equal(Guid.Parse("fb4d2be5-903d-49b3-85b0-af02d448f24a"), database.ID);
        Assert.Equal("tiledb", database.Name);
        Assert.Equal("TileDataComponent", database.DatabaseType);
        Assert.Equal(4, database.ComponentDatabase.Count);

        AssertTile(database, "waterTile", WaterId, TileType.Water, hasCollider: true);
        AssertTile(database, "stoneTile", StoneId, TileType.Stone, hasCollider: true);
        AssertTile(database, "grassTile", GrassId, TileType.Grass, hasCollider: true);
        AssertTile(database, "airTile", AirId, TileType.Air, hasCollider: false);
    }

    private static void AssertTile(
        Engine.Core.Collections.Database database,
        string name,
        Guid assetId,
        TileType tileType,
        bool hasCollider)
    {
        var row = database.GetComponent(name);
        Assert.NotNull(row);
        Assert.Equal(assetId, row.AssetID);

        var tile = Assert.IsType<TileDataComponent>(row);
        Assert.Equal(name, tile.DisplayName);
        Assert.Equal(tileType, tile.TileType);
        Assert.Equal(hasCollider, tile.hasCollider);
    }
}
