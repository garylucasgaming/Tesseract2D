using Engine.Core.Collections;
using Engine.Core.ECS;
using Engine.Core.ECS.Components;
using Engine.Core.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Engine.Core.Serialization
{
    public class DatabaseDataDTO
    {
        public string ID
        {
            get; set;
        }
        public string DataType
        {
            get; set;
        }
        public string Name
        {
            get; set;
        }
        // Key: Human-readable Name String, Value: Serialized component field/property map
        public Dictionary<string, Dictionary<string, object>> DataEntries { get; set; } = new Dictionary<string, Dictionary<string, object>>();
    }

    public static class DatabaseSerializer
    {
        private static readonly ISerializer Serializer = new SerializerBuilder()
            .WithNamingConvention(NullNamingConvention.Instance)
            .Build();

        private static readonly IDeserializer Deserializer = new DeserializerBuilder()
            .WithNamingConvention(NullNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();

        public static void SaveDatabase(Database db, string absoluteFilePath)
        {
            var databaseDto = new DatabaseDataDTO()
            {
                ID = db.ID.ToString(),
                DataType = db.DatabaseType,
                Name = db.Name
            };

            foreach(var kvp in db.ComponentDatabase)
            {
                var component = kvp.Value;

                // 1. Export the component fields/properties into its dictionary map
                var exportedProperties = ComponentSerializer.ExportComponent(component);

                // 2. CRITICAL: Inject the AssetID explicitly inside the property block so it saves as a property field
                exportedProperties["AssetID"] = component.AssetID.ToString();

                // 3. Clean up the DisplayName to act as a valid, readable YAML key title
                string entryKey = string.IsNullOrWhiteSpace(component.DisplayName)
                    ? "UnnamedEntry"
                    : component.DisplayName.Replace(" ", ""); // Strips spaces (e.g. "Water Tile" -> "WaterTile")

                // 4. Collision Guard: If multiple items share a name, append an incremental counter 
                // so YamlDotNet doesn't crash on duplicate keys.
                string uniqueKey = entryKey;
                int counter = 1;
                while(databaseDto.DataEntries.ContainsKey(uniqueKey))
                {
                    uniqueKey = $"{entryKey}_{counter++}";
                }

                databaseDto.DataEntries[uniqueKey] = exportedProperties;
            }

            using(var writer = File.CreateText(absoluteFilePath))
            {
                Serializer.Serialize(writer, databaseDto);
            }
        }

        public static Database? LoadDatabase(string absoluteFilePath)
        {
            if(!File.Exists(absoluteFilePath))
            {
                Log.Error($"[DatabaseSerializer] File not found: {absoluteFilePath}");
                return null;
            }

            using(var reader = File.OpenText(absoluteFilePath))
            {
                var dto = Deserializer.Deserialize<DatabaseDataDTO>(reader);

                var dataBase = new Database
                {
                    ID = Guid.Parse(dto.ID),
                    DatabaseType = dto.DataType,
                    Name = dto.Name
                };

                Log.Info($"[DatabaseSerializer] Loading YAML Database of type: {dataBase.DatabaseType}");

                // Look up the component type definition once for the entire file using TypeResolver
                string typeName = dataBase.DatabaseType;
                Type? compType = Engine.Core.Utilities.TypeResolver.FindType(typeName, typeof(DataComponent));

                if(compType == null)
                {
                    Log.Error($"[DatabaseSerializer] Failed to load database. Type definition for '{typeName}' not found in any loaded assembly.");
                    return null;
                }

                // Process the homogeneous data rows
                foreach(var compKvp in dto.DataEntries)
                {
                    string entryTitle = compKvp.Key;
                    var propertyMap = compKvp.Value;

                    Guid assetGuid = Guid.Empty;
                    bool assetGuidFound = false;

                    if(propertyMap.TryGetValue("AssetID", out object? idObj) && idObj != null)
                    {
                        if(Guid.TryParse(idObj.ToString(), out assetGuid))
                        {
                            assetGuidFound = true;
                        }
                    }

                    if(!assetGuidFound)
                    {
                        Log.Warning($"[DatabaseSerializer] Entry '{entryTitle}' was missing a valid internal 'AssetID'. Automatically generating a recovery GUID.");
                        assetGuid = Guid.NewGuid();
                    }

                    if(Activator.CreateInstance(compType) is DataComponent newComp)
                    {
                        ComponentSerializer.ImportComponent(newComp, propertyMap);
                        newComp.AssetID = assetGuid;
                        dataBase.ComponentDatabase[newComp.AssetID] = newComp;
                    }
                }

                return dataBase;
            }
        }
    }
}