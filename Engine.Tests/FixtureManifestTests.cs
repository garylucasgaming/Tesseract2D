using Engine.Core.Serialization;
using System.Text.Json;
using Xunit;

namespace Engine.Tests;

public class FixtureManifestTests
{
    [Fact]
    public void FixtureManifestIsPopulatedAndUsesRelativePaths()
    {
        var json = File.ReadAllText(FixturePaths.Get("Content/ProjectManifest.db"));
        var manifest = JsonSerializer.Deserialize<ProjectManifest>(json)!;

        Assert.Equal("FontsFixture", manifest.ProjectName);
        Assert.Equal("Main", manifest.LastUsedScene);
        Assert.Equal("Desktop", Assert.Single(manifest.TargetPlatforms));
        Assert.Equal(4, manifest.AssetRegistry.Count);
        Assert.Contains("Content/Assets/Scenes/Main.scene", manifest.AssetRegistry.Values);
        Assert.Contains("Content/Assets/Scenes/Main_TestMap.yaml", manifest.AssetRegistry.Values);
        Assert.Contains("Content/Assets/Databases/tiledb.database", manifest.AssetRegistry.Values);
        Assert.All(manifest.AssetRegistry.Values, path => Assert.False(Path.IsPathRooted(path)));
    }

    [Fact]
    public void EmptyManifestStillProducesDocumentedDefaults()
    {
        var manifest = JsonSerializer.Deserialize<ProjectManifest>("{}")!;

        Assert.Equal("1.0.0", manifest.EngineVersion);
        Assert.Equal("New Project", manifest.ProjectName);
        Assert.Equal(string.Empty, manifest.LastUsedScene);
        Assert.Equal("Desktop", Assert.Single(manifest.TargetPlatforms));
        Assert.Empty(manifest.AssetRegistry);
    }

    [Fact]
    public void CreateInitialManifestWritesSelectedPlatforms()
    {
        var projectRoot = Path.Combine(Path.GetTempPath(), $"t2d-manifest-{Guid.NewGuid():N}");
        Directory.CreateDirectory(Path.Combine(projectRoot, "Content"));

        try
        {
            ProjectManifest.CreateInitialManifest(
                projectRoot,
                "Scratch",
                new List<string> { "Desktop", "Web" });

            var json = File.ReadAllText(Path.Combine(projectRoot, "Content", "ProjectManifest.db"));
            var manifest = JsonSerializer.Deserialize<ProjectManifest>(json)!;

            Assert.Equal("Scratch", manifest.ProjectName);
            Assert.Equal(new[] { "Desktop", "Web" }, manifest.TargetPlatforms);
            Assert.Equal("1.0.0", manifest.EngineVersion);
        }
        finally
        {
            if(Directory.Exists(projectRoot))
                Directory.Delete(projectRoot, recursive: true);
        }
    }
}
