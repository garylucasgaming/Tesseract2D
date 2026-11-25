using System;
using System.Collections.Generic;
using Engine.Core.ECS;
using Engine.Core.Serialization;

public static class GameObjectSerializer
{
    public static EntityDataDto ExportGameObject(GameObject go)
    {
        var dto = new EntityDataDto
        {
            Name = go.Name,
            Id = go.Id.ToString(),
            ParentId = go.Parent != null ? go.Parent.Id.ToString() : Guid.Empty.ToString(),
            Tags = go.tags != null ? new List<string>(go.tags) : new List<string>()
        };

        foreach(var kvp in go.Components)
        {
            dto.Components[kvp.Key.Name] = ComponentSerializer.ExportComponent(kvp.Value);
        }

        return dto;
    }

    public static GameObject ImportGameObject(EntityDataDto dto) => ImportGameObject(dto, strict: false);

    public static GameObject ImportGameObject(EntityDataDto dto, bool strict)
    {
        SerializationVersion.Validate(dto.Version, "Scene entity", dto.Name);

        Guid entityId;
        if(!Guid.TryParse(dto.Id, out entityId))
        {
            if(strict)
            {
                throw new InvalidOperationException($"[GameObjectSerializer] Entity '{dto.Name}' has an invalid Id '{dto.Id}'.");
            }

            Engine.Core.Utilities.Log.Warning($"[GameObjectSerializer] Entity '{dto.Name}' has an invalid Id '{dto.Id}'. Generating a recovery GUID.");
            entityId = Guid.NewGuid();
        }

        Guid parentId = Guid.Empty;
        if(!string.IsNullOrEmpty(dto.ParentId) && !Guid.TryParse(dto.ParentId, out parentId))
        {
            if(strict)
            {
                throw new InvalidOperationException($"[GameObjectSerializer] Entity '{dto.Name}' has an invalid ParentId '{dto.ParentId}'.");
            }

            Engine.Core.Utilities.Log.Warning($"[GameObjectSerializer] Entity '{dto.Name}' has an invalid ParentId '{dto.ParentId}'. Treating it as a root entity.");
            parentId = Guid.Empty;
        }

        var go = new GameObject
        {
            Name = dto.Name,
            Id = entityId,
            ParentId = parentId,
            tags = dto.Tags ?? new List<string>()
        };

        foreach(var compKvp in dto.Components)
        {
            string typeName = compKvp.Key;

            // Replaces the multi-assembly loop with the centralized TypeResolver
            Type? compType = Engine.Core.Utilities.TypeResolver.FindType(typeName, typeof(GameComponent));

            // Process and instantiate if a matching runtime target is recovered
            if(compType != null)
            {
                if(Activator.CreateInstance(compType) is GameComponent newComp)
                {
                    ComponentSerializer.ImportComponent(newComp, compKvp.Value);
                    newComp.gameObject = go;

                    if(go.Components.ContainsKey(compType))
                        go.Components[compType] = newComp;
                    else
                        go.AddComponent(newComp);
                }
            }
            else
            {
                if(strict)
                {
                    throw new InvalidOperationException($"[GameObjectSerializer] Unknown component type '{typeName}' on entity '{dto.Name}'.");
                }

                Engine.Core.Utilities.Log.Warning($"[Serialization] Skipped component: {typeName}. Type definition not found in any loaded assembly.");
            }
        }

        return go;
    }
}