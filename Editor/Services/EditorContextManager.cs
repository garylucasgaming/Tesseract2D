using Engine.Core.ECS;
using Engine.Core.Runtime;
using Engine.Core.Serialization;
using Engine.Core.Utilities;
using System;
using System.IO;

namespace Engine.Core.Serialization
{
    /// <summary>
    /// Editor-facing static bridge over the engine's injectable
    /// <see cref="EngineContext"/>. Kept so the frozen WinForms editor does not
    /// need a large call-site rewrite while engine code stops depending on it.
    /// </summary>
    public static class EditorContextManager
    {
        public static EngineContext Context => EngineContext.Current;

        /// <summary>
        /// Gets the absolute path on disk to the currently loaded project root directory.
        /// Returns null if no project is currently open.
        /// </summary>
        public static string? CurrentProjectRoot
        {
            get => Context.CurrentProjectRoot;
            set => Context.CurrentProjectRoot = value;
        }

        public static string? CurrentProjectName
        {
            get => Context.CurrentProjectName;
            set => Context.CurrentProjectName = value;
        }

        public static ProjectManifest? CurrentProjectManifest
        {
            get => Context.CurrentProjectManifest;
            set => Context.CurrentProjectManifest = value;
        }

        public static bool PlayState
        {
            get => Context.PlayState;
            set => Context.PlayState = value;
        }

        public static int SelectedTileIndex
        {
            get => Context.SelectedTileIndex;
            set => Context.SelectedTileIndex = value;
        }

        /// <summary>
        /// Helper flag to easily check if the editor has completed its loading sequence.
        /// </summary>
        public static bool IsProjectLoaded => Context.IsProjectLoaded;

        // --- Standard Subdirectory Quick-Pointers ---

        public static string ContentPath => Context.ContentPath;
        public static string BinPath => Context.BinPath;
        public static string AssetsPath => Context.AssetsPath;
        public static string ProjectSettingsPath => Context.ProjectSettingsPath;
        public static string LibraryPath => Context.LibraryPath;

        public static string ScenesPath => Context.ScenesPath;
        public static string ScriptsPath => Context.ScriptsPath;

        public static string PrefabsPath => Context.PrefabsPath;

        public static string AudioPath => Context.AudioPath;

        public static string DatabasePath => Context.DatabasePath;

        public static string MaterialsPath => Context.MaterialsPath;

        public static string TexturesPath => Context.TexturesPath;

        /// <summary>
        /// Gets or sets the live, in-memory runtime scene currently active in the editor workspace.
        /// </summary>
        public static GameScene? ActiveLoadedScene
        {
            get => Context.ActiveScene;
            set => Context.ActiveScene = value;
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
            CurrentProjectName = Path.GetFileName(projectRootPath);

            Log.Info($"[Editor Context] Active project workspace successfully mounted to: {CurrentProjectRoot}");
        }

        /// <summary>
        /// Closes the active project context and prepares the workspace to return to the launcher state.
        /// </summary>
        public static void CloseProjectContext()
        {
            CurrentProjectRoot = null;
            ActiveLoadedScene = null;
            Log.Info("[Editor Context] Project context wiped. Returned to baseline state.");
        }

        /// <summary>
        /// Resolves a relative path from the engine environment into a fully absolute path the OS can read.
        /// </summary>
        public static string ResolveToAbsolutePath(string relativePath) =>
            Context.ResolveToAbsolutePath(relativePath);

        /// <summary>
        /// Converts a full system path back into a clean, relative path safe for saving into database files.
        /// </summary>
        public static string GetRelativePath(string absolutePath) =>
            Context.GetRelativePath(absolutePath);

        public static string GetPath(params string[] segments) => Context.GetPath(segments);
    }
}
