using Engine.Core.Runtime;
using Xunit;

namespace Engine.Tests;

public sealed class ProjectPathTests
{
    [Fact]
    public void ScriptSourceRootIsCanonicalContentAssetsDirectory()
    {
        string projectRoot = Path.Combine("D:", "Projects", "Fonts");

        Assert.Equal(
            Path.Combine(projectRoot, "Content", "Assets"),
            ProjectPaths.GetScriptSourceRoot(projectRoot));
    }

    [Fact]
    public void GameplayProjectPathUsesSourceGameplayDirectory()
    {
        string projectRoot = Path.Combine("D:", "Projects", "Fonts");

        Assert.Equal(
            Path.Combine(projectRoot, "Source", "Fonts.Gameplay", "Fonts.Gameplay.csproj"),
            ProjectPaths.GetGameplayProjectPath(projectRoot, "Fonts"));
    }

    [Fact]
    public void GameplayOutputDirectoryMatchesCompilerTargetFramework()
    {
        string projectRoot = Path.Combine("D:", "Projects", "Fonts");

        Assert.Equal(
            Path.Combine(projectRoot, "Source", "Fonts.Gameplay", "bin", "Debug", "net8.0"),
            ProjectPaths.GetGameplayOutputDirectory(projectRoot, "Fonts"));
    }

    [Fact]
    public void GameplayAssemblyPathMatchesCompilerOutput()
    {
        string projectRoot = Path.Combine("D:", "Projects", "Fonts");

        Assert.Equal(
            Path.Combine(projectRoot, "Source", "Fonts.Gameplay", "bin", "Debug", "net8.0", "Fonts.Gameplay.dll"),
            ProjectPaths.GetGameplayAssemblyPath(projectRoot, "Fonts"));
    }
}
