using Engine.Core.ECS;
using Engine.Core.ECS.Systems;
using Engine.Core.GamePlay;
using Engine.Core.Runtime;
using Engine.Core.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using Tommy;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Engine.Core.Serialization
{


    public class SceneDataDto
    {
        public string SceneName
        {
            get; set;
        }
        public string Id
        {
            get; set;
        }

        // Replaced inline MapDataDto lists with separate map file references
        public List<string> TileMapFiles { get; set; } = new List<string>();
        public int ActiveMapIndex { get; set; } = 0;

        public List<string> Managers { get; set; } = new List<string>();
        public List<string> Systems { get; set; } = new List<string>();
        public List<EntityDataDto> Entities { get; set; } = new List<EntityDataDto>();
    }

    // A clean representation of an individual GameObject
    public class EntityDataDto
    {
        public string Name
        {
            get; set;
        }
        public string Id
        {
            get; set;
        }
        public string ParentId
        {
            get; set;
        }
        public List<string> Tags { get; set; } = new List<string>();

        // Key: Component Type Name (e.g., "TransformComponent"), Value: Field/Property key-value state
        public Dictionary<string, Dictionary<string, object>> Components { get; set; } = new Dictionary<string, Dictionary<string, object>>();
    }

    public static class SceneSerializer
    {
        private static readonly ISerializer Serializer = new SerializerBuilder()
            .WithNamingConvention(NullNamingConvention.Instance)
            .Build();

        private static readonly IDeserializer Deserializer = new DeserializerBuilder()
            .WithNamingConvention(NullNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();

        private static Type ResolveType(string typeName)
        {
            if(string.IsNullOrEmpty(typeName))
                return null;

            Type type = Type.GetType(typeName);
            if(type != null)
                return type;

            foreach(var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                type = assembly.GetType(typeName);
                if(type != null)
                    return type;

                try
                {
                    foreach(var t in assembly.GetTypes())
                    {
                        if(t.FullName.Equals(typeName, StringComparison.OrdinalIgnoreCase) ||
                           t.Name.Equals(typeName, StringComparison.OrdinalIgnoreCase))
                        {
                            return t;
                        }
                    }
                }
                catch
                {
                    // Ignore assembly reflection errors
                }
            }
            return null;
        }

        public static void SaveScene(GameScene scene, string absoluteFilePath)
        {
            string sceneDir = Path.GetDirectoryName(absoluteFilePath) ?? string.Empty;
            if(!string.IsNullOrEmpty(sceneDir) && !Directory.Exists(sceneDir))
            {
                Directory.CreateDirectory(sceneDir);
            }

            var sceneDto = new SceneDataDto
            {
                SceneName = scene.SceneName,
                Id = scene.Id.ToString(),
                TileMapFiles = new List<string>()
            };

            // Serialize each map independently via TileMapSerializer
            foreach(var map in scene.SceneMaps)
            {
                string safeMapName = string.IsNullOrEmpty(map.MapName) ? "UntitledMap" : map.MapName.Replace(" ", "_");
                string mapFileName = $"{scene.SceneName}_{safeMapName}.map.yaml";
                string mapFilePath = Path.Combine(sceneDir, mapFileName);

                TileMapSerializer.SaveMap(map, mapFilePath);
                sceneDto.TileMapFiles.Add(mapFileName);
            }

            // Track active map index safely using GameScene's ActiveMapIndex
            sceneDto.ActiveMapIndex = Math.Clamp(scene.ActiveMapIndex, 0, Math.Max(0, scene.SceneMaps.Count - 1));

            // Serialize Managers
            sceneDto.Managers = new List<string>();
            foreach(var manager in scene.Managers.GetRegisteredManagers())
            {
                string typeName = manager.GetType().AssemblyQualifiedName ?? manager.GetType().FullName;
                sceneDto.Managers.Add(typeName);
            }

            // Serialize Custom Systems
            sceneDto.Systems = new List<string>();
            foreach(var system in scene.Systems._systemEntityCache.Keys)
            {
                var sysType = system.GetType();
                if(sysType != typeof(TransformSystem) &&
                   sysType != typeof(SpriteRenderSystem) &&
                   sysType != typeof(PhysicsSystem) &&
                   sysType != typeof(ScriptComponentSystem) &&
                   sysType != typeof(UIRenderSystem) &&
                   sysType != typeof(UILayoutSystem) &&
                   sysType != typeof(UIInputSystem))
                {
                    string typeName = sysType.AssemblyQualifiedName ?? sysType.FullName;
                    sceneDto.Systems.Add(typeName);
                }
            }

            // Serialize Entities
            sceneDto.Entities = new List<EntityDataDto>();
            foreach(var entity in scene.Entities.GetSerializableEntities())
            {
                sceneDto.Entities.Add(GameObjectSerializer.ExportGameObject(entity));
            }

            using(var writer = File.CreateText(absoluteFilePath))
            {
                Serializer.Serialize(writer, sceneDto);
            }
            Log.Info($"[SceneSerializer] Saved Scene to {absoluteFilePath}");
        }

        public static GameScene LoadScene(string absoluteFilePath)
        {
            string sceneDir = Path.GetDirectoryName(absoluteFilePath) ?? string.Empty;

            using(var reader = File.OpenText(absoluteFilePath))
            {
                var sceneDto = Deserializer.Deserialize<SceneDataDto>(reader);

                var scene = new GameScene
                {
                    SceneName = sceneDto.SceneName,
                    Id = Guid.Parse(sceneDto.Id)
                };

                // Reconstruct Maps by loading individual tilemap files
                scene.SceneMaps.Clear();
                if(sceneDto.TileMapFiles != null && sceneDto.TileMapFiles.Count > 0)
                {
                    foreach(var mapFileName in sceneDto.TileMapFiles)
                    {
                        string mapFilePath = Path.Combine(sceneDir, mapFileName);
                        if(File.Exists(mapFilePath))
                        {
                            var map = TileMapSerializer.LoadMap(mapFilePath);
                            scene.SceneMaps.Add(map);
                        }
                        else
                        {
                            Log.Error($"[SceneSerializer] Tilemap file not found: {mapFilePath}");
                        }
                    }

                    // Assign active map index directly; the property setter handles bounds-clamping safely
                    scene.ActiveMapIndex = sceneDto.ActiveMapIndex;
                }

                // Reconstruct Managers
                if(sceneDto.Managers != null)
                {
                    foreach(string managerTypeName in sceneDto.Managers)
                    {
                        Type managerType = ResolveType(managerTypeName);
                        if(managerType != null && managerType.IsSubclassOf(typeof(GameManager)) && !managerType.IsAbstract)
                        {
                            try
                            {
                                if(Activator.CreateInstance(managerType) is GameManager managerInstance)
                                {
                                    managerInstance.ContextScene = scene;
                                    scene.Managers.AddManager(managerInstance);
                                }
                            }
                            catch(Exception ex)
                            {
                                Log.Error($"[SceneSerializer] Failed to instantiate manager '{managerTypeName}': {ex.Message}");
                            }
                        }
                    }
                }

                // Reconstruct Custom Systems
                if(sceneDto.Systems != null)
                {
                    foreach(string systemTypeName in sceneDto.Systems)
                    {
                        Type systemType = ResolveType(systemTypeName);
                        if(systemType != null && systemType.IsSubclassOf(typeof(GameSystem)) && !systemType.IsAbstract)
                        {
                            try
                            {
                                if(Activator.CreateInstance(systemType) is GameSystem systemInstance)
                                {
                                    scene.Systems.AddSystem(systemInstance);
                                }
                            }
                            catch(Exception ex)
                            {
                                Log.Error($"[SceneSerializer] Failed to instantiate system '{systemTypeName}': {ex.Message}");
                            }
                        }
                    }
                }

                Log.Info($"[LoadScene] Loading YAML Scene: {scene.SceneName}");

                var idToEntityMap = new Dictionary<Guid, GameObject>();
                var entityList = new List<GameObject>();

                // Pass 1: Instantiate GameObjects & Load Primitives
                foreach(var entityDto in sceneDto.Entities)
                {
                    var entity = GameObjectSerializer.ImportGameObject(entityDto);
                    entityList.Add(entity);
                    idToEntityMap[entity.Id] = entity;
                }

                // Pass 2: Reconstruct Hierarchy Trees
                foreach(var entity in entityList)
                {
                    if(entity.ParentId != Guid.Empty && idToEntityMap.TryGetValue(entity.ParentId, out var parentEntity))
                    {
                        parentEntity.AddChild(entity);
                        entity.SetParent(parentEntity);
                    }
                }

                // Pass 3: Fill Scene Container
                foreach(var entity in entityList)
                {
                    scene.AddGameObject(entity);
                }

                // Pass 4: Resolve Component GameObject & Component References
                foreach(var entityDto in sceneDto.Entities)
                {
                    if(Guid.TryParse(entityDto.Id, out Guid goId) && idToEntityMap.TryGetValue(goId, out var entity))
                    {
                        foreach(var compKvp in entityDto.Components)
                        {
                            string typeName = compKvp.Key;
                            foreach(var liveCompKvp in entity.Components)
                            {
                                if(liveCompKvp.Key.Name.Equals(typeName, StringComparison.OrdinalIgnoreCase))
                                {
                                    ComponentSerializer.ResolveComponentReferences(liveCompKvp.Value, compKvp.Value, idToEntityMap);
                                    break;
                                }
                            }
                        }
                    }
                }

                return scene;
            }
        }
    }
}