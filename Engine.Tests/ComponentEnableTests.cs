using Engine.Core.ECS;
using Xunit;

namespace Engine.Tests;

public class ComponentEnableTests
{
    [Fact]
    public void GenericAddComponentEnablesComponentAndFiresOnEnabledOnce()
    {
        var gameObject = new GameObject();
        var component = gameObject.AddComponent<CountingComponent>();

        Assert.True(component.isEnabled);
        Assert.Equal(1, component.EnabledCount);
        Assert.Equal(0, component.DisabledCount);
    }

    [Fact]
    public void NonGenericAddComponentEnablesComponentAndFiresOnEnabledOnce()
    {
        var gameObject = new GameObject();
        var component = new CountingComponent();

        gameObject.AddComponent(component);

        Assert.True(component.isEnabled);
        Assert.Equal(1, component.EnabledCount);
        Assert.Equal(0, component.DisabledCount);
    }

    [Fact]
    public void SettingIsEnabledToTheSameValueDoesNotRefireHooks()
    {
        var component = new CountingComponent();

        component.isEnabled = true;
        component.isEnabled = true;
        component.isEnabled = false;
        component.isEnabled = false;

        Assert.Equal(1, component.EnabledCount);
        Assert.Equal(1, component.DisabledCount);
    }

    [Fact]
    public void GenericAddRegistersComponentBeforeOnEnabled()
    {
        var gameObject = new GameObject();

        var component = gameObject.AddComponent<SelfAwareComponent>();

        Assert.True(component.SawSelfDuringEnable);
    }

    [Fact]
    public void NonGenericAddRegistersComponentBeforeOnEnabled()
    {
        var gameObject = new GameObject();
        var component = new SelfAwareComponent();

        gameObject.AddComponent(component);

        Assert.True(component.SawSelfDuringEnable);
    }

    [Fact]
    public void DuplicateNonGenericAddDoesNotRunEnableHook()
    {
        var gameObject = new GameObject();
        gameObject.AddComponent<CountingComponent>();
        var duplicate = new CountingComponent();

        gameObject.AddComponent(duplicate);

        Assert.False(duplicate.isEnabled);
        Assert.Equal(0, duplicate.EnabledCount);
    }

    private sealed class CountingComponent : GameComponent
    {
        public CountingComponent()
        {
        }

        public int EnabledCount { get; private set; }
        public int DisabledCount { get; private set; }

        public override void OnEnabled() => EnabledCount++;
        public override void OnDisabled() => DisabledCount++;
    }

    private sealed class SelfAwareComponent : GameComponent
    {
        public SelfAwareComponent()
        {
        }

        public bool SawSelfDuringEnable { get; private set; }

        public override void OnEnabled()
        {
            SawSelfDuringEnable = gameObject?.GetComponent<SelfAwareComponent>() != null;
        }
    }
}
