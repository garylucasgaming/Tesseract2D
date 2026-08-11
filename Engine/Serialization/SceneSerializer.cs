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
    public class MapDataDto
    {
        public int Width
        {
            get; set;
        }
        public int Height
        {
            get; set;
        }
        public int TileSize
        {
            get; set;
        }
        public int LayerOrder
        {
            get; set;
        }
        public string TilesetPath
        {
            get; set;
        } = string.Empty;
        public List<int> GridFlattened { get; set; } = new List<int>();
    }

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
        public MapDataDto SceneMap
        {
            get; set;
        }
        public List<MapDataDto> SceneMaps { get; set; } = new List<MapDataDto>();
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
            .WithNamingConvention(NullNamingConvention.Instance) // Keeps variable case clean
            .Build();

        private static readonly IDeserializer Deserializer = new DeserializerBuilder()
            .WithNamingConvention(NullNamingConvention.Instance)
            .IgnoreUnmatchedProperties() // Prevents throwing if extra meta fields exist
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
                    // Ignore assembly reflection errors on dynamic/restricted assemblies
                }
            }

            return null;
        }

        public static void SaveScene(GameScene scene, string absoluteFilePath)
        {
            var sceneDto = new SceneDataDto
            {
                SceneName = scene.SceneName,
                Id = scene.Id.ToString()
            };

            // Serialize Map List
            sceneDto.SceneMaps = new List<MapDataDto>();
            foreach(var map in scene.SceneMaps)
            {
                var mapDto = new MapDataDto
                {
                    Width = map.Width,
                    Height = map.Height,
                    TileSize = map.TileSize,
                    LayerOrder = map.LayerOrder,
                    TilesetPath = map.TileSetPath ?? string.Empty,
                    GridFlattened = new List<int>(map.Width * map.Height)
                };

                for(int x = 0; x < map.Width; x++)
                {
                    for(int y = 0; y < map.Height; y++)
                    {
                        mapDto.GridFlattened.Add(map.Grid[x, y]);
                    }
                }

                sceneDto.SceneMaps.Add(mapDto);
            }

            // Track active map index
            sceneDto.ActiveMapIndex = scene.SceneMaps.IndexOf(scene.SceneMap);
            if(sceneDto.ActiveMapIndex < 0)
                sceneDto.ActiveMapIndex = 0;

            // Backward compatibility field for single SceneMap
            if(scene.SceneMap != null)
            {
                var activeMap = scene.SceneMap;
                sceneDto.SceneMap = new MapDataDto
                {
                    Width = activeMap.Width,
                    Height = activeMap.Height,
                    TileSize = activeMap.TileSize,
                    LayerOrder = activeMap.LayerOrder,
                    TilesetPath = activeMap.TileSetPath ?? string.Empty,
                    GridFlattened = new List<int>(activeMap.Width * activeMap.Height)
                };
                for(int x = 0; x < activeMap.Width; x++)
                {
                    for(int y = 0; y < activeMap.Height; y++)
                    {
                        sceneDto.SceneMap.GridFlattened.Add(activeMap.Grid[x, y]);
                    }
                }
            }

            // Serialize Managers
            sceneDto.Managers = new List<string>();
            foreach(var manager in scene.Managers.GetRegisteredManagers())
            {
                string typeName = manager.GetType().AssemblyQualifiedName ?? manager.GetType().FullName;
                sceneDto.Managers.Add(typeName);
            }

            // Serialize Custom Systems (excluding core built-in systems)
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
            foreach(var entity in scene.Entities.GetSerializableEntities())
            {
                sceneDto.Entities.Add(GameObjectSerializer.ExportGameObject(entity));
            }

            using(var writer = File.CreateText(absoluteFilePath))
            {
                Serializer.Serialize(writer, sceneDto);
            }
        }

        public static GameScene LoadScene(string absoluteFilePath)
        {
            using(var reader = File.OpenText(absoluteFilePath))
            {
                var sceneDto = Deserializer.Deserialize<SceneDataDto>(reader);

                var scene = new GameScene
                {
                    SceneName = sceneDto.SceneName,
                    Id = Guid.Parse(sceneDto.Id)
                };

                // Reconstruct Map List
                scene.SceneMaps.Clear();
                if(sceneDto.SceneMaps != null && sceneDto.SceneMaps.Count > 0)
                {
                    foreach(var mapDto in sceneDto.SceneMaps)
                    {
                        var map = new Map(mapDto.Width, mapDto.Height)
                        {
                            TileSize = mapDto.TileSize,
                            LayerOrder = mapDto.LayerOrder,
                            TileSetPath = mapDto.TilesetPath ?? string.Empty
                        };

                        int index = 0;
                        for(int x = 0; x < mapDto.Width; x++)
                        {
                            for(int y = 0; y < mapDto.Height; y++)
                            {
                                if(index < mapDto.GridFlattened.Count)
                                {
                                    map.Grid[x, y] = mapDto.GridFlattened[index++];
                                }
                            }
                        }

                        scene.SceneMaps.Add(map);
                    }

                    if(sceneDto.ActiveMapIndex >= 0 && sceneDto.ActiveMapIndex < scene.SceneMaps.Count)
                    {
                        scene.SceneMap = scene.SceneMaps[sceneDto.ActiveMapIndex];
                    }
                    else if(scene.SceneMaps.Count > 0)
                    {
                        scene.SceneMap = scene.SceneMaps[0];
                    }
                }
                else if(sceneDto.SceneMap != null) // Fallback for legacy single-map scenes
                {
                    var mapDto = sceneDto.SceneMap;
                    var map = new Map(mapDto.Width, mapDto.Height)
                    {
                        TileSize = mapDto.TileSize,
                        LayerOrder = mapDto.LayerOrder,
                        TileSetPath = mapDto.TilesetPath ?? string.Empty
                    };

                    int index = 0;
                    for(int x = 0; x < mapDto.Width; x++)
                    {
                        for(int y = 0; y < mapDto.Height; y++)
                        {
                            if(index < mapDto.GridFlattened.Count)
                            {
                                map.Grid[x, y] = mapDto.GridFlattened[index++];
                            }
                        }
                    }

                    scene.SceneMaps.Add(map);
                    scene.SceneMap = map;
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

                            // Match component by type on live GameObject instance
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