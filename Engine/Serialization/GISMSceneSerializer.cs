using System;
using System.IO;
using System.Collections.Generic;
using Engine.Core.ECS;
using GISM.Core.Serializer;
using GISM.Core.Parser;
using Engine.Core.Utilities;

namespace Engine.Core.Serialization
{
    public static class GISMSceneSerializer
    {
        public static void SaveScene(GameScene scene, string absoluteFilePath)
        {
            Log.Info("[GISMSceneSerializer] Executing loop-safe object serialization pass...");

            var options = new GISMSerializerOptions { IsExplicit = true };
            var serializer = new GISMSerializer(options);

            string gismOutput = serializer.Serialize(scene.Entities.GetSerializableEntities());

            File.WriteAllText(absoluteFilePath, gismOutput);
            Log.Info("[GISMSceneSerializer] Engine Scene saved successfully!");
        }

        public static GameScene LoadScene(string absoluteFilePath)
        {
            Log.Info($"[GISMSceneSerializer] Reading GISM Scene File: {absoluteFilePath}...");

            if(!File.Exists(absoluteFilePath))
            {
                throw new FileNotFoundException($"[GISMSceneSerializer] Failed to load scene. File not found: {absoluteFilePath}");
            }

            // 1. Core Environment Parsing Rules
            var settings = new GISMParserSettings
            {
                //DefaultInferredType = typeof(GameObject)
            };
            settings.TypeAssemblies.Add(typeof(GameScene).Assembly);
            settings.TypeAssemblies.Add(typeof(Microsoft.Xna.Framework.Vector2).Assembly);

            // 2. Fire and forget blackbox deserialization!
            string rawGism = File.ReadAllText(absoluteFilePath);
            var deserializer = new GISMDeserializer(settings);
            GISMResult result = deserializer.Deserialize(rawGism);

            // 3. Setup a clean engine scene state
            var loadedScene = new GameScene();

            // Query out exactly what we need using our new typed helper
            List<GameObject> entities = result.GetObjectsOfType<GameObject>();

            // 4. Rebuild parent-child scene graph hierarchies natively
            Log.Info("[GISMSceneSerializer] Rebuilding entity hierarchies...");
            foreach(var entity in entities)
            {
                if(entity.ParentId != null && entity.ParentId != Guid.Empty)
                {
                    var parentEntity = entities.Find(e => e.Id == entity.ParentId);
                    if(parentEntity != null)
                    {
                        parentEntity.AddChild(entity);
                    }
                    else
                    {
                        Log.Warning($"[GISMSceneSerializer] Missing parent ID '{entity.ParentId}' for '{entity.Name}'");
                    }
                }
            }

            // 5. Populate our engine scene container
            foreach(var entity in entities)
            {
                if(entity == null)
                    continue;
                loadedScene.AddGameObject(entity);
            }

            Log.Info($"[GISMSceneSerializer] Successfully rehydrated {entities.Count} entities into the scene.");
            return loadedScene;
        }
    }
}