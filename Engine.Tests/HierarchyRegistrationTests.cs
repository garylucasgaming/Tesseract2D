using System;
using Engine.Core.ECS;
using Xunit;

namespace Engine.Tests;

public class HierarchyRegistrationTests
{
    [Fact]
    public void AddChildRegistersUnregisteredChildWithSceneAndFiresParentChanged()
    {
        var scene = new GameScene();
        var parent = scene.Spawn("Parent");
        var child = new GameObject { Name = "Child" };
        var events = 0;
        GameObject? eventChild = null;
        GameObject? eventParent = null;
        child.ParentChangedEvent += (c, p) =>
        {
            events++;
            eventChild = c;
            eventParent = p;
        };

        parent.AddChild(child);

        Assert.Same(parent, child.Parent);
        Assert.Equal(parent.Id, child.ParentId);
        Assert.Contains(child, parent.Children);
        Assert.Same(scene, child.ContextScene);
        Assert.True(scene.Entities.HasEntity(child.Id));
        Assert.Equal(1, events);
        Assert.Same(child, eventChild);
        Assert.Same(parent, eventParent);
    }

    [Fact]
    public void AddChildReparentsWithoutDuplicatingRegistration()
    {
        var scene = new GameScene();
        var oldParent = scene.Spawn("Old");
        var newParent = scene.Spawn("New");
        var child = scene.Spawn("Child");
        oldParent.AddChild(child);
        var entityCount = scene.Entities.EntityCount;

        newParent.AddChild(child);

        Assert.Same(newParent, child.Parent);
        Assert.Equal(newParent.Id, child.ParentId);
        Assert.DoesNotContain(child, oldParent.Children);
        Assert.Contains(child, newParent.Children);
        Assert.Equal(entityCount, scene.Entities.EntityCount);
    }

    [Fact]
    public void RemoveChildClearsParentIdAndFiresParentChanged()
    {
        var scene = new GameScene();
        var parent = scene.Spawn("Parent");
        var child = scene.Spawn("Child");
        parent.AddChild(child);
        var events = 0;
        GameObject? eventChild = null;
        GameObject? eventParent = null;
        child.ParentChangedEvent += (c, p) =>
        {
            events++;
            eventChild = c;
            eventParent = p;
        };

        parent.RemoveChild(child);

        Assert.Null(child.Parent);
        Assert.Equal(Guid.Empty, child.ParentId);
        Assert.DoesNotContain(child, parent.Children);
        Assert.Equal(1, events);
        Assert.Same(child, eventChild);
        Assert.Null(eventParent);
    }

    [Fact]
    public void AddChildRejectsSelfAndDescendantCycles()
    {
        var scene = new GameScene();
        var parent = scene.Spawn("Parent");
        var child = scene.Spawn("Child");
        parent.AddChild(child);

        child.AddChild(child);
        child.AddChild(parent);

        Assert.Same(parent, child.Parent);
        Assert.Null(parent.Parent);
        Assert.DoesNotContain(parent, child.Children);
    }
}
