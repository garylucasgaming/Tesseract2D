using Engine.Core.ECS;
using Engine.Core.Serialization;
using System;
using System.IO;

namespace Engine.Core.Runtime
{
    /// <summary>
    /// The engine-facing description of the currently mounted project and session.
    /// Hosts create one context per session; the frozen editor bridges its static
    /// context manager onto this type.
    /// </summary>
    public interface IEngineContext
    {
        string? CurrentProjectRoot { get; }
        string? CurrentProjectName { get; }
        ProjectManifest? CurrentProjectManifest { get; }
        bool IsProjectLoaded { get; }

        bool PlayState { get; set; }
        int SelectedTileIndex { get; set; }

        GameScene? ActiveScene { get; set; }

        string ContentPath { get; }
        string BinPath { get; }
        string AssetsPath { get; }
        string ProjectSettingsPath { get; }
        string LibraryPath { get; }
        string ScenesPath { get; }
        string ScriptsPath { get; }
        string PrefabsPath { get; }
        string AudioPath { get; }
        string DatabasePath { get; }
        string MaterialsPath { get; }
        string TexturesPath { get; }

        string ResolveToAbsolutePath(string relativePath);
        string GetRelativePath(string absolutePath);
        string GetPath(params string[] segments);
    }

    /// <summary>
    /// Mutable, injectable implementation of <see cref="IEngineContext"/>.
    /// </summary>
    public sealed class EngineContext : IEngineContext
    {
        /// <summary>
        /// Compatibility context for code paths that have not been given an
        /// explicit context yet. New hosts should create their own instance.
        /// </summary>
        public static EngineContext Current { get; set; } = new();

        public string? CurrentProjectRoot { get; set; }
        public string? CurrentProjectName { get; set; }
        public ProjectManifest? CurrentProjectManifest { get; set; }

        public bool PlayState { get; set; }
        public int SelectedTileIndex { get; set; } = -1;

        public GameScene? ActiveScene { get; set; }

        public bool IsProjectLoaded => !string.IsNullOrEmpty(CurrentProjectRoot);

        public string ContentPath => GetPath("Content");
        public string BinPath => GetPath(ContentPath, "Bin");
        public string AssetsPath => GetPath("Content", "Assets");
        public string ProjectSettingsPath => GetPath("ProjectSettings");
        public string LibraryPath => GetPath("Library");
        public string ScenesPath => GetPath(AssetsPath, "Scenes");
        public string ScriptsPath => GetPath(AssetsPath, "Scripts");
        public string PrefabsPath => GetPath(AssetsPath, "Prefabs");
        public string AudioPath => GetPath(AssetsPath, "Audio");
        public string DatabasePath => GetPath(AssetsPath, "Databases");
        public string MaterialsPath => GetPath(AssetsPath, "Materials");
        public string TexturesPath => GetPath(AssetsPath, "Textures");

        public string ResolveToAbsolutePath(string relativePath)
        {
            if(!IsProjectLoaded)
            {
                throw new InvalidOperationException(
                    "[Engine Context] Cannot resolve paths while no project context is mounted.");
            }

            var cleanRelative = relativePath.Replace('\\', '/').TrimStart('/')
                .Replace('/', Path.DirectorySeparatorChar);
            return Path.Combine(CurrentProjectRoot!, cleanRelative);
        }

        public string GetRelativePath(string absolutePath)
        {
            if(!IsProjectLoaded)
                return absolutePath;

            var cleanRoot = CurrentProjectRoot!.Replace('\\', '/').TrimEnd('/') + "/";
            var cleanAbsolute = absolutePath.Replace('\\', '/');

            if(cleanAbsolute.StartsWith(cleanRoot, StringComparison.OrdinalIgnoreCase))
                return cleanAbsolute[cleanRoot.Length..];

            return absolutePath;
        }

        public string GetPath(params string[] segments)
        {
            if(!IsProjectLoaded)
                return string.Empty;

            var path = CurrentProjectRoot!;
            foreach(var segment in segments)
            {
                path = Path.Combine(path, segment);
            }
            return path;
        }
    }
}
