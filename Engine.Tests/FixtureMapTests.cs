using Engine.Core.Serialization;
using Xunit;

namespace Engine.Tests;

public class FixtureMapTests
{
    [Fact]
    public void FontsMapFixtureHasGridAndTileDataDictionary()
    {
        var map = FixtureYaml.Load<MapDataDto>("Content/Assets/Scenes/Main_TestMap.yaml");

        Assert.Equal("TestMap", map.MapName);
        Assert.Equal(25, map.Width);
        Assert.Equal(19, map.Height);
        Assert.Equal(32, map.TileSize);
        Assert.Equal("Assets/Textures/tilesettest2.png", map.TilesetPath);
        Assert.Equal("tiledb", map.TileDatabaseName);
        Assert.True(map.IsEnabled);
        Assert.Equal(map.Width * map.Height, map.GridFlattened.Count);
        Assert.Contains(1, map.GridFlattened);
        Assert.Contains(3, map.GridFlattened);
        Assert.Contains(4, map.GridFlattened);

        Assert.Equal(4, map.TileProperties.Count);
        Assert.Equal(0, map.TileProperties[0]);
        Assert.Equal(1, map.TileProperties[1]);
        Assert.Equal(3, map.TileProperties[2]);
        Assert.Equal(4, map.TileProperties[3]);

        Assert.Equal(4, map.TileIndexDataDictionary.Count);
        var assetIds = map.TileIndexDataDictionary.Values
            .Select(value => GetString(value, "AssetID"))
            .ToHashSet();

        Assert.Contains("cdbd33eb-bd9f-4091-826c-5bde028fb442", assetIds);
        Assert.Contains("3c200462-c7dd-4522-bba3-baf6bceef243", assetIds);
        Assert.Contains("e62fef10-8292-4f35-be5a-0016d405ad9b", assetIds);
        Assert.Contains("79378f0e-3c95-4c49-8f3e-695b32ec4c77", assetIds);
    }

    private static string? GetString(object? entry, string key)
    {
        if(entry is IDictionary<object, object> objectDict &&
           objectDict.TryGetValue(key, out var objectValue))
            return objectValue?.ToString();

        if(entry is IDictionary<string, object> stringDict &&
           stringDict.TryGetValue(key, out var stringValue))
            return stringValue?.ToString();

        return null;
    }
}
