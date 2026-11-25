using Engine.Core.ECS;
using Engine.Core.Runtime;
using Xunit;

namespace Engine.Tests;

public sealed class EngineContextTests
{
    [Fact]
    public void SceneUsesInjectedContextInsteadOfEditorStaticState()
    {
        var fixtureRoot = Path.Combine(AppContext.BaseDirectory, "TestContent", "Fonts");
        var context = new EngineContext
        {
            CurrentProjectRoot = fixtureRoot,
            CurrentProjectName = "Fonts",
            PlayState = true
        };

        var scene = new GameScene(context);

        Assert.Same(context, scene.Context);
        Assert.Equal(fixtureRoot, scene.Context.CurrentProjectRoot);
        Assert.Single(scene.Database.Databases);
        Assert.Equal("tiledb", scene.Database.Databases[0].Name);
    }

    [Fact]
    public void ContextExposesProjectPathHelpers()
    {
        var root = Path.Combine(Path.GetTempPath(), "t2d-context-" + Guid.NewGuid().ToString("N"));
        var context = new EngineContext { CurrentProjectRoot = root };

        Assert.Equal(Path.Combine(root, "Content", "Assets", "Databases"), context.DatabasePath);
        Assert.Equal(Path.Combine(root, "Content", "Bin"), context.BinPath);
        Assert.Equal(Path.Combine(root, "Content", "Assets", "Scenes"), context.ScenesPath);
        Assert.Equal(
            Path.Combine(root, "Content", "Assets", "Scenes", "Main.scene"),
            context.ResolveToAbsolutePath("Content/Assets/Scenes/Main.scene"));
    }
}
