using Engine.Core.ECS;
using Engine.Core.ECS.Components;
using Engine.Core.GamePlay;
using Engine.Core.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Engine.Core.Serialization
{
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

        public List<int> GridFlattened { get; set; } = new List<int>();
        public Dictionary<int, int> TileProperties { get; set; } = new Dictionary<int, int>();

        // Use object dictionary for deserialization safety so YamlDotNet doesn't throw on polymorphic DataComponent reflection
        public Dictionary<int, object> TileIndexDataDictionary { get; set; } = new Dictionary<int, object>();
    }

    public static class TileMapSerializer
    {
        private static readonly ISerializer Serializer = new SerializerBuilder()
            .WithNamingConvention(NullNamingConvention.Instance)
            .IgnoreFields()
            .Build();

        private static readonly IDeserializer Deserializer = new DeserializerBuilder()
            .WithNamingConvention(NullNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();

        public static void SaveMap(Map map, string targetPath)
        {
            if(map == null)
                return;

            try
            {
                var dto = new MapDataDto
                {
                    MapName = map.MapName,
                    Width = map.Width,
                    Height = map.Height,
                    TileSize = map.TileSize,
                    LayerOrder = map.LayerOrder,
                    TilesetPath = map.TileSetPath,
                    IsEnabled = map.IsEnabled,
                    TileDatabaseName = map.TileDatabaseName,
                    TileProperties = map.TileProperties,
                    GridFlattened = map.GridFlattened
                };

                string dir = Path.GetDirectoryName(targetPath);
                if(!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                using(var writer = File.CreateText(targetPath))
                {
                    Serializer.Serialize(writer, dto);
                }

                Log.Info($"[TileMapSerializer] Map saved successfully to {targetPath}");
            }
            catch(Exception ex)
            {
                Log.Error($"[TileMapSerializer] Failed to save map to {targetPath}: {ex.Message}");
            }
        }

        public static Map LoadMap(string absoluteFilePath, GameScene activeScene = null)
        {
            if(!File.Exists(absoluteFilePath))
            {
                Log.Error($"[TileMapSerializer] Map file not found at: {absoluteFilePath}");
                return null;
            }

            try
            {
                MapDataDto dto;
                using(var reader = File.OpenText(absoluteFilePath))
                {
                    dto = Deserializer.Deserialize<MapDataDto>(reader);
                }

                if(dto == null)
                    return null;

                // 1. Create instance with dimensions
                var map = new Map(dto.Width, dto.Height);

                // 2. Set primitive metadata FIRST
                map.ContextScene = activeScene;
                map.MapName = dto.MapName;
                map.TileSize = dto.TileSize;
                map.LayerOrder = dto.LayerOrder;
                map.TileSetPath = dto.TilesetPath;
                map.IsEnabled = dto.IsEnabled;
                map.TileDatabaseName = dto.TileDatabaseName;
                map.TileProperties = dto.TileProperties ?? new Dictionary<int, int>();

                // 3. Resolve database reference against active scene BEFORE rebuilding grids
                if(activeScene != null)
                {
                    map.ResolveDatabase(activeScene);
                }
                map.TileIndexDataDictionary = new Dictionary<int, DataComponent>();
                if(dto.TileIndexDataDictionary != null && map.TileDatabase?.ComponentDatabase != null)
                {
                    foreach(var kvp in dto.TileIndexDataDictionary)
                    {
                        int tileIdx = kvp.Key;
                        var rawObj = kvp.Value;

                        if(rawObj is DataComponent directComp)
                        {
                            map.TileIndexDataDictionary[tileIdx] = directComp;
                        }
                        else if(rawObj is IDictionary<object, object> yamlDict)
                        {
                            // Extract identifier/name fields saved in the YAML dictionary
                            string compName = yamlDict.TryGetValue("DisplayName", out var dn) ? dn?.ToString() :
                                             (yamlDict.TryGetValue("Name", out var n) ? n?.ToString() : string.Empty);

                            string assetIdStr = yamlDict.TryGetValue("AssetID", out var id) ? id?.ToString() : string.Empty;

                            // Find matching live DataComponent from the map's resolved TileDatabase
                            var matchedDbComponent = map.TileDatabase.ComponentDatabase.Values.FirstOrDefault(c =>
                                (Guid.TryParse(assetIdStr, out var g) && g != Guid.Empty && c.AssetID == g) ||
                                (!string.IsNullOrEmpty(compName) && c.DisplayName.Equals(compName, StringComparison.OrdinalIgnoreCase)) ||
                                (!string.IsNullOrEmpty(compName) && c.DisplayName.Equals(compName, StringComparison.OrdinalIgnoreCase))
                            );

                            if(matchedDbComponent != null)
                            {
                                map.TileIndexDataDictionary[tileIdx] = matchedDbComponent;
                            }
                        }
                    }
                }
                // 4. Assign GridFlattened LAST (triggers UnflattenGrid & RebuildTileDataGrid with active database reference)
                map.GridFlattened = dto.GridFlattened ?? new List<int>();

                Log.Info($"[TileMapSerializer] Loaded map '{map.MapName}' from {absoluteFilePath}");
                return map;
            }
            catch(Exception ex)
            {
                Log.Error($"[TileMapSerializer] Failed to load map from {absoluteFilePath}: {ex.ToString()}");
                return null;
            }
        }
    }
}