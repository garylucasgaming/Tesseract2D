using Engine.Core.Serialization; // Access to EditorContextManager
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Engine.Core.Utilities
{
    public enum AssetType
    {
        Texture,
        Audio,
        Font,
        Effect
    }

    public static class AssetManager
    {
        private static readonly Dictionary<AssetType, string[]> SupportedExtensions = new()
        {
            { AssetType.Texture, new[] { ".png", ".jpg", ".jpeg", ".tga", ".bmp" } },
            { AssetType.Audio, new[] { ".wav", ".mp3", ".ogg", ".wma" } },
            { AssetType.Font, new[] { ".spritefont" } },
            { AssetType.Effect, new[] { ".fx" } }
        };

        public static string GetSubfolderName(AssetType type) => type switch
        {
            AssetType.Texture => "Textures",
            AssetType.Audio => "Audio",
            AssetType.Font => "Fonts",
            AssetType.Effect => "Effects",
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };

        /// <summary>
        /// Recursively strips all file extensions from a path.
        /// Example: "Enemies/goblin.png" -> "Enemies/goblin"
        ///          "Enemies/goblin.png.xnb" -> "Enemies/goblin"
        /// </summary>
        public static string StripExtension(string path)
        {
            if(string.IsNullOrEmpty(path))
                return string.Empty;

            string clean = path.Replace('\\', '/');
            while(!string.IsNullOrEmpty(Path.GetExtension(clean)))
            {
                clean = Path.ChangeExtension(clean, null);
            }
            return clean;
        }

        /// <summary>
        /// Scans raw assets and returns fully stripped relative keys.
        /// </summary>
        public static List<string> GetAvailableKeys(AssetType type)
        {
            var keys = new List<string>();

            if(!EditorContextManager.IsProjectLoaded)
                return keys;

            string subfolder = GetSubfolderName(type);
            string targetDir = Path.Combine(EditorContextManager.AssetsPath, subfolder);

            if(!Directory.Exists(targetDir))
                return keys;

            if(!SupportedExtensions.TryGetValue(type, out var extensions))
                return keys;

            var files = Directory.GetFiles(targetDir, "*.*", SearchOption.AllDirectories)
                .Where(file => extensions.Contains(Path.GetExtension(file).ToLower()));

            foreach(var file in files)
            {
                string relativePath = Path.GetRelativePath(targetDir, file);
                string cleanKey = StripExtension(relativePath);
                keys.Add(cleanKey);
            }

            return keys;
        }

        /// <summary>
        /// Translates a key to the relative path expected by ContentManager.Load.
        /// Does NOT include any file extensions[cite: 18].
        /// Example: "Enemies/goblin" -> "Assets/Textures/Enemies/goblin"
        /// </summary>
        public static string GetContentRelativePath(string assetKey, AssetType type)
        {
            if(string.IsNullOrEmpty(assetKey))
                return string.Empty;

            string cleanKey = StripExtension(assetKey);
            string subfolder = GetSubfolderName(type);

            // Because Content Builder compiles with SourceDirectory = Content/Assets,
            // the output is relative to that folder (e.g. "Textures/Capture")
            return $"{subfolder}/{cleanKey}";
        }

        /// <summary>
        /// Translates a key to its physical .xnb file on disk.
        /// </summary>
        public static string GetAbsolutePhysicalPath(string assetKey, AssetType type)
        {
            if(string.IsNullOrEmpty(assetKey))
                return string.Empty;

            string cleanKey = StripExtension(assetKey);
            string subfolder = GetSubfolderName(type);

            return Path.Combine(
                EditorContextManager.BinPath,
                "Content",
                "Assets",
                subfolder,
                cleanKey + ".xnb"
            );
        }
    }
}