using Engine.Core.ECS;
using Engine.Core.Runtime;
using Xunit;

namespace Engine.Tests;

public sealed class HeadlessHostTests
{
    [Fact]
    public void HeadlessHostRunsFiveFramesWithManagerLifecycle()
    {
        var fixtureRoot = Path.Combine(AppContext.BaseDirectory, "TestContent", "Fonts");
        var context = new EngineContext { CurrentProjectRoot = fixtureRoot };
        var session = new SessionGlobals(context, new NullInputSource());
        var host = new GameHost(context, session, new NullFrameRenderer());

        var scene = host.LoadScene(
            Path.Combine(fixtureRoot, "Content", "Assets", "Scenes", "Main.scene"));
        var manager = new CountingGameManager();
        scene.Managers.AddManager(manager);

        host.RunFrames(5, 1f / 60f);
        host.Shutdown();

        Assert.Same(scene, host.Scene);
        Assert.Same(scene, manager.ContextScene);
        Assert.Equal(1, manager.InitializeCount);
        Assert.Equal(5, manager.UpdateCount);
        Assert.Equal(1, manager.ShutdownCount);
    }

    [Fact]
    public void HeadlessHostSkipsInactiveManagerUpdatesButShutsItDown()
    {
        var fixtureRoot = Path.Combine(AppContext.BaseDirectory, "TestContent", "Fonts");
        var context = new EngineContext { CurrentProjectRoot = fixtureRoot };
        var session = new SessionGlobals(context, new NullInputSource());
        var host = new GameHost(context, session, new NullFrameRenderer());

        var scene = host.LoadScene(
            Path.Combine(fixtureRoot, "Content", "Assets", "Scenes", "Main.scene"));
        var manager = new CountingGameManager { IsActive = false };
        scene.Managers.AddManager(manager);

        host.RunFrames(3, 1f / 60f);
        host.Shutdown();

        Assert.Equal(0, manager.InitializeCount);
        Assert.Equal(0, manager.UpdateCount);
        Assert.Equal(1, manager.ShutdownCount);
    }

    private sealed class CountingGameManager : GameManager
    {
        public int InitializeCount { get; private set; }
        public int UpdateCount { get; private set; }
        public int ShutdownCount { get; private set; }

        public override void Initialize() => InitializeCount++;

        public override void Update(float deltaTime) => UpdateCount++;

        public override void Shutdown() => ShutdownCount++;
    }
}
