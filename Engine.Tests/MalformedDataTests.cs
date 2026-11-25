using Engine.Core.ECS.Components;
using Engine.Core.Serialization;
using Xunit;

namespace Engine.Tests;

public class MalformedDataTests
{
    [Fact]
    public void MalformedEntityAndParentIdsRecoverWithoutThrowing()
    {
        var dto = new EntityDataDto
        {
            Name = "Broken",
            Id = "not-a-guid",
            ParentId = "also-not-a-guid",
            Tags = null!,
            Components = new Dictionary<string, Dictionary<string, object>>
            {
                [nameof(PrimitiveFixtureComponent)] = new()
                {
                    ["Count"] = 7
                }
            }
        };

        var entity = GameObjectSerializer.ImportGameObject(dto);

        Assert.NotEqual(Guid.Empty, entity.Id);
        Assert.NotEqual(Guid.Parse("00000000-0000-0000-0000-000000000000"), entity.Id);
        Assert.Equal(Guid.Empty, entity.ParentId);
        Assert.Empty(entity.tags);
        Assert.Equal(7, entity.GetComponent<PrimitiveFixtureComponent>()!.Count);
    }

    [Fact]
    public void MalformedDatabaseIdAndMissingAssetIdsRecoverWithoutThrowing()
    {
        var path = Path.Combine(Path.GetTempPath(), $"t2d-db-{Guid.NewGuid():N}.database");
        File.WriteAllText(path, """
            ID: not-a-guid
            DataType: TileDataComponent
            Name: BadDb
            DataEntries:
              BrokenTile:
                DisplayName: BrokenTile
                TileType: Water
              AnotherTile:
                DisplayName: AnotherTile
                TileType: Stone
                AssetID: bad-guid
            """);

        try
        {
            var database = DatabaseSerializer.LoadDatabase(path);

            Assert.NotNull(database);
            Assert.NotEqual(Guid.Empty, database.ID);
            Assert.Equal("BadDb", database.Name);
            Assert.Equal("TileDataComponent", database.DatabaseType);
            Assert.Equal(2, database.ComponentDatabase.Count);

            var broken = database.GetComponent("BrokenTile");
            Assert.NotNull(broken);
            Assert.NotEqual(Guid.Empty, broken.AssetID);
            Assert.Equal(TileType.Water, Assert.IsType<TileDataComponent>(broken).TileType);

            var another = database.GetComponent("AnotherTile");
            Assert.NotNull(another);
            Assert.NotEqual(Guid.Empty, another.AssetID);
            Assert.Equal(TileType.Stone, Assert.IsType<TileDataComponent>(another).TileType);
        }
        finally
        {
            if(File.Exists(path))
                File.Delete(path);
        }
    }
}
