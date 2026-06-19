using System;
using System.IO;
using Engine.Core.ECS;
using Engine.Core.Utilities;

namespace Engine.Core.Serialization
{
    /// <summary>
    /// Tracks the live state, directories, and path-resolution mechanics of the currently opened engine project.
    /// </summary>
    public static class EditorContextManager
    {
        /// <summary>
        /// Gets the absolute path on disk to the currently loaded project root directory.
        /// Returns null if no project is currently open.
        /// </summary>
        public static string? CurrentProjectRoot
        {
            get; private set;
        }

        /// <summary>
        /// Helper flag to easily check if the editor has completed its loading sequence.
        /// </summary>
        public static bool IsProjectLoaded => !string.IsNullOrEmpty(CurrentProjectRoot);

        // --- Standard Subdirectory Quick-Pointers ---
        public static string AssetsPath => GetProjectSubFolder("Assets");
        public static string ContentPath => GetProjectSubFolder("Content");
        public static string ProjectSettingsPath => GetProjectSubFolder("ProjectSettings");
        public static string LibraryPath => GetProjectSubFolder("Library");

        /// Gets or sets the live, in-memory runtime scene currently active in the editor workspace.
        /// </summary>
        public static GameScene? ActiveLoadedScene
        {
            get; set;
        }

      

        /// <summary>
        /// Sets the current active project context workspace directory path.
        /// </summary>
        public static void OpenProjectContext(string projectRootPath)
        {
            if(!Directory.Exists(projectRootPath))
            {
                Log.Error($"[Editor Context Error] Cannot point workspace to non-existent directory: {projectRootPath}");
                return;
            }

            CurrentProjectRoot = projectRootPath;
            Log.Info($"[Editor Context] Active project workspace successfully mounted to: {CurrentProjectRoot}");
        }

        /// <summary>
        /// Closes the active project context and prepares the workspace to return to the launcher state.
        /// </summary>
        public static void CloseProjectContext()
        {
            CurrentProjectRoot = null;
            ActiveLoadedScene = null; // 👈 Scrub the live memory pointer on close!
            Log.Info("[Editor Context] Project context wiped. Returned to baseline state.");
        }

        /// <summary>
        /// Resolves a relative path from the engine environment into a fully absolute path the OS can read.
        /// Example: "Content/Scenes/Untitled.scene" -> "D:/MyGames/ProjectAlpha/Content/Scenes/Untitled.scene"
        /// </summary>
        public static string ResolveToAbsolutePath(string relativePath)
        {
            if(!IsProjectLoaded)
            {
                throw new InvalidOperationException("[Editor Context Error] Cannot resolve paths while no project context is mounted.");
            }

            // Clean up backslashes/forward slashes just in case
            string cleanRelative = relativePath.Replace('\\', '/').TrimStart('/');
            return Path.Combine(CurrentProjectRoot!, cleanRelative);
        }

        /// <summary>
        /// Converts a full system path back into a clean, relative path safe for saving into database JSON files.
        /// Example: "D:/MyGames/ProjectAlpha/Assets/Textures/stone.png" -> "Assets/Textures/stone.png"
        /// </summary>
        public static string GetRelativePath(string absolutePath)
        {
            if(!IsProjectLoaded)
                return absolutePath;

            string cleanRoot = CurrentProjectRoot!.Replace('\\', '/').TrimEnd('/') + "/";
            string cleanAbsolute = absolutePath.Replace('\\', '/');

            if(cleanAbsolute.StartsWith(cleanRoot, StringComparison.OrdinalIgnoreCase))
            {
                return cleanAbsolute.Substring(cleanRoot.Length);
            }

            return absolutePath; // Return original if it doesn't live inside the project layout
        }

        private static string GetProjectSubFolder(string folderName)
        {
            if(!IsProjectLoaded)
                return string.Empty;
            return Path.Combine(CurrentProjectRoot!, folderName);
        }
    }
}