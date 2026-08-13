using Engine.Core.Collections;
using Engine.Core.ECS.Components;
using Engine.Core.GamePlay;
using Engine.Core.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Engine.Core.Serialization
{
    public class TileDataEntryDto
    {
        public int X
        {
            get; set;
        }
        public int Y
        {
            get; set;
        }
        public string ComponentTypeName { get; set; } = string.Empty;
        public Dictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();
    }

    public class MapDataDto
    {
        public string MapName { get; set; } = string.Empty;
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
        public string TilesetPath { get; set; } = string.Empty;
        public bool IsEnabled { get; set; } = true;
        public string TileDatabaseName { get; set; } = string.Empty;

        public Dictionary<int, int> TileProperties { get; set; } = new Dictionary<int, int>();
        public List<int> GridFlattened { get; set; } = new List<int>();
        public List<TileDataEntryDto> TileDataEntries { get; set; } = new List<TileDataEntryDto>();
    }
    public class TileMapSerializer
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
                    // Ignore restricted assemblies
                }
            }
            return null;
        }

        public static void SaveMap(Map map, string absoluteFilePath)
        {
            var mapDto = new MapDataDto
            {
                MapName = map.MapName,
                Width = map.Width,
                Height = map.Height,
                TileSize = map.TileSize,
                LayerOrder = map.LayerOrder,
                TilesetPath = map.TileSetPath ?? string.Empty,
                IsEnabled = map.IsEnabled,
                TileDatabaseName = map.TileDatabase?.Name ?? string.Empty,
                TileProperties = map.TileProperties != null ? new Dictionary<int, int>(map.TileProperties) : new Dictionary<int, int>(),
                GridFlattened = new List<int>(map.Width * map.Height)
            };

            for(int x = 0; x < map.Width; x++)
            {
                for(int y = 0; y < map.Height; y++)
                {
                    mapDto.GridFlattened.Add(map.Grid[x, y]);

                    // Serialize TileDataGrid entry if present
                    var dataComp = map.TileDataGrid?[x, y];
                    if(dataComp != null)
                    {
                        var compType = dataComp.GetType();
                        string typeName = compType.AssemblyQualifiedName ?? compType.FullName;

                        var propDict = new Dictionary<string, object>();
                        foreach(var prop in compType.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance))
                        {
                            if(prop.CanRead && prop.CanWrite && !prop.IsDefined(typeof(DatabaseIgnoreAttribute), true))
                            {
                                var val = prop.GetValue(dataComp);
                                if(val != null)
                                {
                                    propDict[prop.Name] = val;
                                }
                            }
                        }

                        mapDto.TileDataEntries.Add(new TileDataEntryDto
                        {
                            X = x,
                            Y = y,
                            ComponentTypeName = typeName,
                            Properties = propDict
                        });
                    }
                }
            }

            using(var writer = File.CreateText(absoluteFilePath))
            {
                Serializer.Serialize(writer, mapDto);
            }
            Log.Info($"[TileMapSerializer] Saved map '{map.MapName}' to {absoluteFilePath}");
        }

        public static Map LoadMap(string absoluteFilePath)
        {
            using(var reader = File.OpenText(absoluteFilePath))
            {
                var mapDto = Deserializer.Deserialize<MapDataDto>(reader);

                var map = new Map(mapDto.Width, mapDto.Height)
                {
                    MapName = mapDto.MapName ?? string.Empty,
                    TileSize = mapDto.TileSize,
                    LayerOrder = mapDto.LayerOrder,
                    IsEnabled = mapDto.IsEnabled,
                    TileSetPath = mapDto.TilesetPath ?? string.Empty,
                    TileProperties = mapDto.TileProperties != null ? new Dictionary<int, int>(mapDto.TileProperties) : new Dictionary<int, int>()
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

                // Reconstruct TileDataGrid entries
                if(mapDto.TileDataEntries != null)
                {
                    foreach(var entry in mapDto.TileDataEntries)
                    {
                        if(entry.X >= 0 && entry.X < map.Width && entry.Y >= 0 && entry.Y < map.Height)
                        {
                            Type compType = ResolveType(entry.ComponentTypeName);
                            if(compType != null && typeof(DataComponent).IsAssignableFrom(compType))
                            {
                                try
                                {
                                    if(Activator.CreateInstance(compType) is DataComponent compInstance)
                                    {
                                        foreach(var kvp in entry.Properties)
                                        {
                                            var prop = compType.GetProperty(kvp.Key);
                                            if(prop != null && prop.CanWrite)
                                            {
                                                try
                                                {
                                                    var convertedVal = Convert.ChangeType(kvp.Value, prop.PropertyType);
                                                    prop.SetValue(compInstance, convertedVal);
                                                }
                                                catch
                                                {
                                                    prop.SetValue(compInstance, kvp.Value);
                                                }
                                            }
                                        }
                                        map.TileDataGrid[entry.X, entry.Y] = compInstance;
                                    }
                                }
                                catch(Exception ex)
                                {
                                    Log.Error($"[TileMapSerializer] Failed to instantiate TileDataComponent '{entry.ComponentTypeName}': {ex.Message}");
                                }
                            }
                        }
                    }
                }

                Log.Info($"[TileMapSerializer] Loaded map '{map.MapName}' from {absoluteFilePath}");
                return map;
            }
        }

    }
}
