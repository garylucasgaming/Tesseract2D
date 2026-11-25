using Engine.Core.ECS.Managers;
using Engine.Core.Runtime;
using Microsoft.Xna.Framework;
using Xunit;

namespace Engine.Tests;

public sealed class SessionGlobalsTests
{
    [Fact]
    public void SessionsKeepIndependentInputAndCameras()
    {
        var sourceA = new NullInputSource { MousePosition = new Vector2(10, 20) };
        var sourceB = new NullInputSource { MousePosition = new Vector2(30, 40) };
        var sessionA = new SessionGlobals(new EngineContext(), sourceA);
        var sessionB = new SessionGlobals(new EngineContext(), sourceB);

        sessionA.EditorCamera.Position = new Vector2(1, 2);
        sessionB.EditorCamera.Position = new Vector2(3, 4);

        sessionA.UpdateInput();
        sessionB.UpdateInput();

        Assert.NotSame(sessionA.InputManager, sessionB.InputManager);
        Assert.NotSame(sessionA.EditorCamera, sessionB.EditorCamera);
        Assert.Equal(new Vector2(10, 20), sessionA.InputManager.MousePosition);
        Assert.Equal(new Vector2(30, 40), sessionB.InputManager.MousePosition);
        Assert.Equal(new Vector2(1, 2), sessionA.EditorCamera.Position);
        Assert.Equal(new Vector2(3, 4), sessionB.EditorCamera.Position);

        var systemsA = new SystemsManager(sessionA);
        var systemsB = new SystemsManager(sessionB);

        Assert.Same(sessionA, systemsA.Session);
        Assert.Same(sessionB, systemsB.Session);
        Assert.NotSame(systemsA.uiInputSystem, systemsB.uiInputSystem);
    }
}
