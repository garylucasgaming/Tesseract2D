using Engine.Core.ECS;
using Engine.Core.ECS.Components;
using Engine.Core.ECS.Components.UI;
using Engine.Core.Serialization;
using Engine.Core.Utilities;
using Microsoft.Xna.Framework;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using GameComponent = Engine.Core.ECS.GameComponent;

namespace Engine.Tests;

// ComponentSerializer.ShouldIgnore matches by attribute type name, not by CLR type identity.
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public sealed class IgnoreAttribute : Attribute
{
}

public enum FixtureKind
{
    Alpha,
    Beta
}

public sealed class PrimitiveFixtureComponent : GameComponent
{
    public int Count;
    public string Label { get; set; } = string.Empty;
    public FixtureKind Kind;
    public Vector2 Position;
    public List<string> Items = new();

    [Ignore]
    public string Skipped { get; set; } = "must-not-serialize";
}

public sealed class ReferenceFixtureComponent : GameComponent
{
    public GameObject? TargetObject;
    public GameComponent? TargetComponent;
    public List<GameObject> Targets = new();
    public Engine.Core.Runtime.GameEvent? OnActivate;
}

public class BaseFixtureComponent : GameComponent
{
}

public sealed class DerivedFixtureComponent : BaseFixtureComponent
{
}

public sealed class OtherFixtureComponent : GameComponent
{
}

// Deliberately not a GameComponent/GameManager: used to pin resolver divergence.
public sealed class ResolverProbe
{
}

public sealed class FixtureTileDataComponent : DataComponent
{
    public override string DisplayName { get; set; } = string.Empty;
}

internal static class FixturePaths
{
    private static readonly string Root = Path.Combine(AppContext.BaseDirectory, "TestContent", "Fonts");

    public static string Get(string relativePath) =>
        Path.Combine(Root, relativePath.Replace('/', Path.DirectorySeparatorChar));
}

internal static class FixtureYaml
{
    private static readonly IDeserializer Deserializer = new DeserializerBuilder()
        .WithNamingConvention(NullNamingConvention.Instance)
        .IgnoreUnmatchedProperties()
        .Build();

    public static T Load<T>(string relativePath)
    {
        using var reader = File.OpenText(FixturePaths.Get(relativePath));
        return Deserializer.Deserialize<T>(reader);
    }
}

internal static class FixtureScene
{
    public static List<GameObject> ImportAll(
        SceneDataDto dto,
        Action<List<GameObject>>? beforeResolve = null)
    {
        var entities = dto.Entities.Select(GameObjectSerializer.ImportGameObject).ToList();
        var idToEntityMap = new Dictionary<Guid, GameObject>();

        for(var i = 0; i < entities.Count; i++)
            idToEntityMap[entities[i].Id] = entities[i];

        beforeResolve?.Invoke(entities);

        for(var i = 0; i < dto.Entities.Count; i++)
        {
            foreach(var dtoComponent in dto.Entities[i].Components)
            {
                foreach(var liveComponent in entities[i].Components)
                {
                    if(!liveComponent.Key.Name.Equals(dtoComponent.Key, StringComparison.OrdinalIgnoreCase))
                        continue;

                    ComponentSerializer.ResolveComponentReferences(
                        liveComponent.Value,
                        dtoComponent.Value,
                        idToEntityMap);
                    break;
                }
            }
        }

        return entities;
    }
}
