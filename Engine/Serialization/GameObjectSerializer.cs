using System;
using Engine.Core.ECS;
using Engine.Core.Serialization;
using Tommy;

public static class GameObjectSerializer
{
    public static TomlTable ExportGameObject(GameObject go)
    {
        var table = new TomlTable();
        table["name"] = go.Name;
        table["id"] = go.Id.ToString();

        if(go.Parent != null)
            table["parent_id"] = go.Parent.Id.ToString();

        foreach(var kvp in go.Components)
        {
            var exportedCompValue = ComponentSerializer.ExportComponent(kvp.Value);

            // 👇 FIX: Force the component properties to layout beautifully inline
            // inside the GameObject's main table without creating breaking dot-notation headers
            exportedCompValue.IsInline = true;

            table[kvp.Key.Name] = exportedCompValue;
        }

        return table;
    }

    public static GameObject ImportGameObject(TomlTable table)
    {
        var go = new GameObject();
        go.Name = table["name"];
        go.Id = Guid.Parse(table["id"].ToString());

        foreach(var entry in table.Keys)
        {
            if(entry == "name" || entry == "id" || entry == "parent_id")
                continue;

            Type compType = Type.GetType($"Engine.Core.ECS.Components.{entry}, Engine.Core");

            if(compType != null)
            {
                var newComp = Activator.CreateInstance(compType) as GameComponent;
                TomlNode componentNode = table[entry];

                // Handles inline table values smoothly
                if(componentNode is TomlTable ||  componentNode.IsTable)
                {
                    ComponentSerializer.ImportComponent(newComp, (TomlTable) componentNode);
                }

                newComp.gameObject = go;

                if(go.Components.ContainsKey(compType))
                {
                    go.Components[compType] = newComp;
                }
                else
                {
                    go.Components.Add(compType, newComp);
                }

                
            }
        }

        return go;
    }
}