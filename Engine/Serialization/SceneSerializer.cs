using System;
using System.IO;
using System.Numerics;
using System.Text.Json;
using Engine.Core.ECS;
using Engine.Core.Utilities;

namespace Engine.Core.Serialization
{
    public static class SceneSerializer
    {
        // Setup central JSON rules with our custom polymorphic converter injected
        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            WriteIndented = true,
            Converters = {
        new GameComponentConverter(),
        new TypeDictionaryKeyConverter() // 👈 ADD THIS LINE HERE
    },
            PropertyNameCaseInsensitive = true
        };



        /// <summary>
        /// Serializes a live runtime GameScene out to a clean, flat data layout on disk.
        /// </summary>
        public static void SaveScene(GameScene scene, string absoluteFilePath)
        {
            try
            {
                var contract = new SceneFileContract
                {
                    SceneName = scene.SceneName,
                    Id = scene.Id,
                    Entities = scene.Entities.GetSerializableEntities()
                };

                string jsonStr = JsonSerializer.Serialize(contract, _jsonOptions);
                File.WriteAllText(absoluteFilePath, jsonStr);

                Log.Info($"[Scene Serializer] Successfully baked scene '{scene.SceneName}' to disk at: {absoluteFilePath}");
            }
            catch(Exception ex)
            {
                Log.Error($"[Scene Serializer Error] Failed to serialize scene data! Reason: {ex.Message}");
                throw;
            }
        }



        /// <summary>
        /// Reads a flat JSON file contract and converts it into a live, fully linked ECS runtime scene.
        /// </summary>
        public static GameScene LoadScene(string absoluteFilePath)
        {
            if(!File.Exists(absoluteFilePath))
            {
                throw new FileNotFoundException($"[Scene Serializer Error] Target scene file not found at: {absoluteFilePath}");
            }

            try
            {

                string jsonStr = File.ReadAllText(absoluteFilePath);
                var contract = JsonSerializer.Deserialize<SceneFileContract>(jsonStr, _jsonOptions);

                // 1. Build a brand-new live operational context container instance
                GameScene newScene = new GameScene
                {
                    SceneName = contract.SceneName,
                    Id = contract.Id
                };

                if(contract == null)
                {
                    throw new JsonException($"[Scene Serializer Error] Deserialization returned a null contract for: {absoluteFilePath}");
                }

                

                // 2. Initialize the operational managers (Systems, Entities, Events)
                newScene.InitializeManagers();
                // 3. PASS 1: Seed all raw flat GameObjects back into the EntityManager database.
                foreach(var rawEntity in contract.Entities)
                {
                    // Register it to the system
                    newScene.Entities.AddEntity(rawEntity);

                    // 👇 FIX: Fetch the actual operational instance back out of the live database registry!
                    var liveEntity = newScene.Entities.Find(rawEntity.Id);

                    if(liveEntity != null && liveEntity.Transform != null)
                    {
                        // Bind the live component horizontally to the live engine entity
                        liveEntity.Transform.Owner = liveEntity;
                    }

                    }

                // 4. PASS 2: Repair the live object graph relationships. 
                foreach(var rawEntity in contract.Entities)
                {
                    // 👇 FIX: Use the live database entity here too!
                    var liveEntity = newScene.Entities.Find(rawEntity.Id);

                    var transform = liveEntity?.Transform;
                    var oldPosition = transform?.WorldPosition;
                    var oldOffset = new Vector2(transform?.XOffset ?? 0, transform?.YOffset ?? 0);


                    if(liveEntity == null)
                        continue;

                    if(rawEntity.ParentId.HasValue)
                    {
                        var parentObject = newScene.Entities.Find(rawEntity.ParentId.Value);
                        if(parentObject != null)
                        {
                            liveEntity.SetParent(parentObject);
                            var child = parentObject.Children.Find(c => c.Id == liveEntity.Id);
                            }
                        else
                        {
                             }
                    }
                    foreach(var component in liveEntity.Components.Values)
                    {
                        component.Owner = liveEntity; // Rebind the component to the live entity
                    }

                    transform.X = oldPosition.Value.X;
                    transform.XOffset = 0;
                    transform.XOffset = oldOffset.X;
                    transform.Y = oldPosition.Value.Y;
                    transform.YOffset = 0;
                    transform.YOffset = oldOffset.Y;
                }

                 return newScene;
            }
            catch(Exception ex)
            {
                Log.Error($"[Scene Serializer Error] Failed to read or parse target scene data! Reason: {ex.Message}");
                throw;
            }
        }
    }
}