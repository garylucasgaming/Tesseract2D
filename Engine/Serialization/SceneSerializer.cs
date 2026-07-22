using System;
using System.Collections.Generic;
using System.IO;
using Tommy;
using Engine.Core.ECS;
using Engine.Core.Utilities;
using System.ComponentModel;
using YamlDotNet.Serialization.NamingConventions;
using YamlDotNet.Serialization;

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

        public static void SaveScene(GameScene scene, string absoluteFilePath)
        {
            var sceneDto = new SceneDataDto
            {
                SceneName = scene.SceneName,
                Id = scene.Id.ToString()
            };

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