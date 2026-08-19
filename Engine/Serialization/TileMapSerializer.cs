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
        public Dictionary<int, DataComponent> TileIndexDataDictionary { get; set; } = new Dictionary<int, DataComponent>();
    }

    public static class TileMapSerializer
    {
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
                    TileIndexDataDictionary = map.TileIndexDataDictionary,
                    GridFlattened = map.GridFlattened
                };

                var serializer = new SerializerBuilder()
                    .WithNamingConvention(PascalCaseNamingConvention.Instance)
                    .IgnoreFields()
                    .Build();

                string yaml = serializer.Serialize(dto);
                File.WriteAllText(targetPath, yaml);
                Log.Info($"[TileMapSerializer] Map saved successfully to {targetPath}");
            }
            catch(Exception ex)
            {
                Log.Error($"[TileMapSerializer] Failed to save map to {targetPath}: {ex.Message}");
            }
        }

        public static Map LoadMap(string relativeFilePath, GameScene activeScene = null)
        {
            string activeProjectDir = EditorContextManager.CurrentProjectRoot ?? string.Empty;
            string absoluteFilePath = relativeFilePath;

            if(!Path.IsPathRooted(relativeFilePath))
            {
                absoluteFilePath = Path.Combine(activeProjectDir, relativeFilePath);
            }

            if(!File.Exists(absoluteFilePath))
            {
                Log.Error($"[TileMapSerializer] Map file not found at: {absoluteFilePath}");
                return null;
            }

            try
            {
                string yaml = File.ReadAllText(absoluteFilePath);
                var deserializer = new DeserializerBuilder()
                    .WithNamingConvention(PascalCaseNamingConvention.Instance)
                    .IgnoreUnmatchedProperties()
                    .Build();

                var dto = deserializer.Deserialize<MapDataDto>(yaml);
                if(dto == null)
                    return null;

                // 1. Create instance with dimensions
                var map = new Map(dto.Width, dto.Height);

                // 2. Set primitive metadata & dictionaries FIRST
                map.MapName = dto.MapName;
                map.TileSize = dto.TileSize;
                map.LayerOrder = dto.LayerOrder;
                map.TileSetPath = dto.TilesetPath;
                map.IsEnabled = dto.IsEnabled;
                map.TileDatabaseName = dto.TileDatabaseName;
                map.TileProperties = dto.TileProperties ?? new Dictionary<int, int>();
                map.TileIndexDataDictionary = dto.TileIndexDataDictionary ?? new Dictionary<int, DataComponent>();

                // 3. Resolve database reference against active scene BEFORE rebuilding grids
                if(activeScene != null)
                {
                    map.ResolveDatabase(activeScene);
                }

                // 4. Assign GridFlattened LAST (triggers UnflattenGrid & RebuildTileDataGrid with active database reference)
                map.GridFlattened = dto.GridFlattened ?? new List<int>();

                Log.Info($"[TileMapSerializer] Loaded map '{map.MapName}' from {absoluteFilePath}");
                return map;
            }
            catch(Exception ex)
            {
                Log.Error($"[TileMapSerializer] Failed to load map from {absoluteFilePath}: {ex.Message}");
                return null;
            }
        }
    }
}