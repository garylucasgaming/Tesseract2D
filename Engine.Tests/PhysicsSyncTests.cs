using System.Linq;
using Engine.Core.ECS;
using Engine.Core.ECS.Components;
using Microsoft.Xna.Framework;
using nkast.Aether.Physics2D.Dynamics;
using Xunit;

namespace Engine.Tests;

public class PhysicsSyncTests
{
    [Fact]
    public void LateSpawnedBodyIsInitializedOnNextTick()
    {
        var scene = new GameScene();
        var first = SpawnKinematic(scene, "First", 0f, 0f);

        scene.TickUpdate(1f / 60f, playModeActive: true);

        var firstBody = first.GetComponent<PhysicsBodyComponent>().physicsBody;
        Assert.NotNull(firstBody);

        var late = SpawnKinematic(scene, "Late", 128f, 0f);
        Assert.Null(late.GetComponent<PhysicsBodyComponent>().physicsBody);

        scene.TickUpdate(1f / 60f, playModeActive: true);

        var lateBody = late.GetComponent<PhysicsBodyComponent>().physicsBody;
        Assert.NotNull(lateBody);
        Assert.Contains(lateBody, scene.Physics.PhysicsSpace.BodyList);
    }

    [Fact]
    public void MovingTransformPushesToBodyUsingPixelsPerMeter()
    {
        var scene = new GameScene();
        var entity = SpawnKinematic(scene, "Moved", 0f, 0f);
        scene.TickUpdate(1f / 60f, playModeActive: true);

        var transform = entity.GetComponent<TransformComponent>();
        var bodyComponent = entity.GetComponent<PhysicsBodyComponent>();
        float ppm = scene.Physics.PixelsPerMeter;

        transform.WorldPosition = new Vector2(ppm * 4f, ppm * 2f);
        scene.TickUpdate(1f / 60f, playModeActive: true);

        Assert.Equal(4f, bodyComponent.physicsBody.Position.X, 3);
        Assert.Equal(2f, bodyComponent.physicsBody.Position.Y, 3);
        Assert.Equal(ppm * 4f, transform.WorldPosition.X, 3);
        Assert.Equal(ppm * 2f, transform.WorldPosition.Y, 3);

        scene.TickUpdate(1f / 60f, playModeActive: true);
        Assert.Equal(4f, bodyComponent.physicsBody.Position.X, 3);
        Assert.Equal(2f, bodyComponent.physicsBody.Position.Y, 3);
    }

    [Fact]
    public void EveryColliderOnBodyGetsInitialized()
    {
        var scene = new GameScene();
        var entity = SpawnKinematic(scene, "DoubleCollider", 0f, 0f);
        entity.AddComponent(new CircleColliderComponent { Radius = 10f });

        // One component instance per exact type is allowed, so the second collider
        // must be a different collider subclass.
        Assert.Equal(2, entity.Components.Values.OfType<ColliderComponent>().Count());

        scene.TickUpdate(1f / 60f, playModeActive: true);

        var body = entity.GetComponent<PhysicsBodyComponent>().physicsBody;
        Assert.NotNull(body);
        Assert.Equal(2, body.FixtureList.Count);
    }

    [Fact]
    public void ColliderAddedAfterFirstTickGetsExactlyOneFixture()
    {
        var scene = new GameScene();
        var entity = SpawnKinematic(scene, "LateCollider", 0f, 0f);

        scene.TickUpdate(1f / 60f, playModeActive: true);
        var body = entity.GetComponent<PhysicsBodyComponent>().physicsBody;
        Assert.NotNull(body);
        Assert.Equal(1, body.FixtureList.Count);

        entity.AddComponent(new CircleColliderComponent { Radius = 10f });
        scene.TickUpdate(1f / 60f, playModeActive: true);
        Assert.Equal(2, body.FixtureList.Count);

        scene.TickUpdate(1f / 60f, playModeActive: true);
        Assert.Equal(2, body.FixtureList.Count);
    }

    private static GameObject SpawnKinematic(GameScene scene, string name, float x, float y)
    {
        var entity = scene.Spawn(name, x, y);
        entity.AddComponent(new PhysicsBodyComponent
        {
            bodyType = BodyType.Kinematic,
            ignoreGravity = true
        });
        entity.AddComponent(new BoxColliderComponent());
        return entity;
    }
}
