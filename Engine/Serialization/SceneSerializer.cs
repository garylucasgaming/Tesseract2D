using System;
using System.Collections.Generic;
using System.IO;
using Tommy;
using Engine.Core.ECS;
using Engine.Core.Utilities;
using System.ComponentModel;

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

                Log.Info($"[LoadScene] Loading scene: {scene.SceneName}");

                if(!table.HasKey("entities"))
                {
                    Log.Info("[LoadScene] ❌ CRITICAL: The 'entities' key does not exist in the TOML file!");
                    return scene;
                }

                var entitiesNode = table["entities"];
                Log.Info($"[LoadScene] 'entities' node type in Tommy: {entitiesNode.GetType().Name}");

                var entitiesTable = entitiesNode.AsTable;
                Log.Info($"[LoadScene] Number of entity keys found: {entitiesTable.Keys.Count()}");

                var idToEntityMap = new Dictionary<Guid, GameObject>();
                var entityList = new List<GameObject>();

                // 1. Pass One: Create all entities
                foreach(var entityKey in entitiesTable.Keys)
                {
                    Log.Info($"[LoadScene] Found entity key: {entityKey}");
                    var entityTable = entitiesTable[entityKey].AsTable;

                    var entity = GameObjectSerializer.ImportGameObject(entityTable);
                    Log.Info($"[LoadScene] Imported entity: {entity.Name} with ID: {entity.Id}");
                    if(entity.ParentId != Guid.Empty)
                    {
                        Log.Info($"[LoadScene] Entity {entity.Name} has parent ID: {entity.ParentId}");
                    }
                    entityList.Add(entity);
                    idToEntityMap[entity.Id] = entity;
                }

                // 2. Restore child parent relationship for each gameobject
                foreach(var entity in entityList)
                {
                    if(entity.ParentId != Guid.Empty && idToEntityMap.TryGetValue(entity.ParentId, out var parentEntity))
                    {
                        parentEntity.AddChild(entity);
                        entity.SetParent(parentEntity);
                    }
                }



                //pass 3 populate scene
                foreach(var e in entityList)
                {
                    scene.AddGameObject(e);
                }

               


                return scene;
            }
        }
    }
}