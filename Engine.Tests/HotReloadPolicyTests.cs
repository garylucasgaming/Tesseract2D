using Engine.Core.ECS;
using Engine.Core.Runtime;
using Xunit;

namespace Engine.Tests;

public sealed class HotReloadPolicyTests
{
    [Fact]
    public void AssemblySwapRequiresSceneReloadAndSceneReloadClearsIt()
    {
        var fixtureRoot = Path.Combine(AppContext.BaseDirectory, "TestContent", "Fonts");
        var context = new EngineContext { CurrentProjectRoot = fixtureRoot };
        var host = new GameHost(context, new SessionGlobals(context, new NullInputSource()), new NullFrameRenderer());
        var states = new System.Collections.Generic.List<HotReloadState>();

        var originalScene = host.LoadScene(
            Path.Combine(fixtureRoot, "Content", "Assets", "Scenes", "Main.scene"));
        host.HotReloadStateChanged += (_, args) => states.Add(args.State);

        host.NotifyAssemblyReloaded();

        Assert.Equal(HotReloadState.ReloadRequired, host.HotReloadState);
        Assert.Equal(new[] { HotReloadState.ReloadRequired }, states);
        Assert.Same(originalScene, host.Scene);

        Assert.True(host.TryExecuteCommand("scene.reload"));

        Assert.Equal(HotReloadState.UpToDate, host.HotReloadState);
        Assert.Equal(new[] { HotReloadState.ReloadRequired, HotReloadState.UpToDate }, states);
        Assert.NotSame(originalScene, host.Scene);
    }

    [Fact]
    public void AttachedSceneWithoutFilePathRefusesReload()
    {
        var context = new EngineContext();
        var session = new SessionGlobals(context, new NullInputSource());
        var host = new GameHost(context, session, new NullFrameRenderer());
        var attached = new GameScene(context, session: session);

        host.AttachScene(attached);
        host.NotifyAssemblyReloaded();

        Assert.False(host.TryExecuteCommand("scene.reload"));
        Assert.Equal(HotReloadState.ReloadRequired, host.HotReloadState);
        Assert.Same(attached, host.Scene);
        Assert.Throws<InvalidOperationException>(() => host.ReloadScene());
    }

    [Fact]
    public void UnknownCommandsAreIgnoredByHost()
    {
        var context = new EngineContext();
        var host = new GameHost(context, new SessionGlobals(context, new NullInputSource()), new NullFrameRenderer());

        Assert.False(host.TryExecuteCommand("scene.delete"));
    }
}
