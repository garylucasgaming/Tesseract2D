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

    public static GameObject ImportGameObject(EntityDataDto dto)
    {
        var go = new GameObject
        {
            Name = dto.Name,
            Id = Guid.Parse(dto.Id),
            ParentId = !string.IsNullOrEmpty(dto.ParentId) ? Guid.Parse(dto.ParentId) : Guid.Empty,
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
                Engine.Core.Utilities.Log.Warning($"[Serialization] Skipped component: {typeName}. Type definition not found in any loaded assembly.");
            }
        }

        return go;
    }
}