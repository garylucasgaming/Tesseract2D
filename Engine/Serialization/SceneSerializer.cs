using System;
using System.Collections.Generic;
using System.IO;
using Tommy;
using Engine.Core.ECS;

namespace Engine.Core.Serialization
{
    public static class SceneSerializer
    {
        public static void SaveScene(GameScene scene, string absoluteFilePath)
        {
            var root = new TomlTable();
            root["scene_name"] = scene.SceneName;
            root["id"] = scene.Id.ToString();

            var entitiesTable = new TomlTable();
            foreach(var entity in scene.Entities.GetSerializableEntities())
            {
                entitiesTable[entity.Id.ToString()] = GameObjectSerializer.ExportGameObject(entity);
            }
            root["entities"] = entitiesTable;

            using(var writer = File.CreateText(absoluteFilePath))
            {
                root.WriteTo(writer);
            }
        }

        public static GameScene LoadScene(string absoluteFilePath)
        {
            using(var reader = File.OpenText(absoluteFilePath))
            {
                var table = TOML.Parse(reader);
                var scene = new GameScene
                {
                    SceneName = table["scene_name"],
                    Id = Guid.Parse(table["id"].ToString())
                };

                Console.WriteLine($"[LoadScene] Loading scene: {scene.SceneName}");

                if(!table.HasKey("entities"))
                {
                    Console.WriteLine("[LoadScene] ❌ CRITICAL: The 'entities' key does not exist in the TOML file!");
                    return scene;
                }

                var entitiesNode = table["entities"];
                Console.WriteLine($"[LoadScene] 'entities' node type in Tommy: {entitiesNode.GetType().Name}");

                var entitiesTable = entitiesNode.AsTable;
                Console.WriteLine($"[LoadScene] Number of entity keys found: {entitiesTable.Keys.Count()}");

                var idToEntityMap = new Dictionary<Guid, GameObject>();

                // 1. Pass One: Create all entities
                foreach(var entityKey in entitiesTable.Keys)
                {
                    Console.WriteLine($"[LoadScene] Found entity key: {entityKey}");
                    var entityTable = entitiesTable[entityKey].AsTable;

                    var entity = GameObjectSerializer.ImportGameObject(entityTable);
                    scene.Entities.AddEntity(entity);
                    idToEntityMap[entity.Id] = entity;
                }

                // 2. Pass Two: Restore Hierarchy
                foreach(var entityKey in entitiesTable.Keys)
                {
                    var entityTable = entitiesTable[entityKey].AsTable;
                    if(!entityTable.HasKey("id"))
                        continue;

                    Guid entityId = Guid.Parse(entityTable["id"].ToString());

                    if(entityTable.HasKey("parent_id") && entityTable["parent_id"] != null)
                    {
                        string parentIdStr = entityTable["parent_id"].ToString();
                        if(!string.IsNullOrEmpty(parentIdStr))
                        {
                            Guid parentId = Guid.Parse(parentIdStr);
                            if(idToEntityMap.TryGetValue(parentId, out var parent))
                            {
                                idToEntityMap[entityId].SetParent(parent);
                            }
                        }
                    }
                }

                
                return scene;
            }
        }
    }
}