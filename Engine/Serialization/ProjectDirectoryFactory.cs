using System;
using System.IO;
using System.Text.Json;
using Engine.Core.ECS;
using Engine.Core.Utilities;

namespace Engine.Core.Serialization
{
    /// <summary>
    /// Responsible for stamping out a unified, multi-layered game project workspace layout on disk.
    /// </summary>
    public static class ProjectDirectoryFactory
    {
        /// <summary>
        /// Creates a brand-new engine project matching our standardized industry-inspired folder architecture.
        /// </summary>
        /// <param name="parentDirectory">The absolute drive path where the project directory will be created.</param>
        /// <param name="projectName">The name of the new game project.</param>
        /// <returns>The absolute path to the newly created project root directory.</returns>
        public static string CreateNewProject(string parentDirectory, string projectName)
        {
            try
            {
                // 1. Calculate and establish the root project directory path
                string projectRoot = Path.Combine(parentDirectory, projectName);
                if(!Directory.Exists(projectRoot))
                {
                    Directory.CreateDirectory(projectRoot);
                }

                // 2. Define the major engine Root Folders
                string assetsRoot = Path.Combine(projectRoot, "Assets");
                string contentRoot = Path.Combine(projectRoot, "Content");
                string projectSettingsRoot = Path.Combine(projectRoot, "ProjectSettings");
                string libraryRoot = Path.Combine(projectRoot, "Library");
                string tempRoot = Path.Combine(projectRoot, "Temp");

                Directory.CreateDirectory(assetsRoot);
                Directory.CreateDirectory(contentRoot);
                Directory.CreateDirectory(projectSettingsRoot);
                Directory.CreateDirectory(libraryRoot);
                Directory.CreateDirectory(tempRoot);

                // 3. Populate sub-folders inside the Assets directory
                Directory.CreateDirectory(Path.Combine(assetsRoot, "Scripts"));
                Directory.CreateDirectory(Path.Combine(assetsRoot, "Textures"));
                Directory.CreateDirectory(Path.Combine(assetsRoot, "Materials"));
                Directory.CreateDirectory(Path.Combine(assetsRoot, "Audio"));
                Directory.CreateDirectory(Path.Combine(assetsRoot, "Scenes"));
                Directory.CreateDirectory(Path.Combine(assetsRoot, "Prefabs"));

                // 4. Populate sub-folders inside our data-driven Content directory
                string dbFolder = Path.Combine(contentRoot, "Databases");
                string sceneDataFolder = Path.Combine(contentRoot, "Scenes");
                string prefabDataFolder = Path.Combine(contentRoot, "Prefabs");

                Directory.CreateDirectory(dbFolder);
                Directory.CreateDirectory(sceneDataFolder);
                Directory.CreateDirectory(prefabDataFolder);

                Log.Info($"[Project Factory] Successfully initialized workspace layouts at: {projectRoot}");

                // 5. Stamp out a blank, fresh Project Manifest JSON database inside the data Content folder
                string manifestPath = Path.Combine(contentRoot, "ProjectManifest.db");
                File.WriteAllText(manifestPath, "{}");
                Log.Info("[Project Factory] Stamped data-link ProjectManifest.db file.");

                // 6. Bake our initial boilerplate scene data asset into Content/Scenes/
                CreateDefaultSceneTemplate(sceneDataFolder);

                return projectRoot;
            }
            catch(Exception ex)
            {
                Log.Error($"[Project Factory Error] Critical failure creating project folders! Reason: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Bakes a simple, standard boilerplate scene layout so the engine can safely initialize a workspace.
        /// </summary>
        private static void CreateDefaultSceneTemplate(string targetSceneFolder)
        {
            try
            {
                string targetFilePath = Path.Combine(targetSceneFolder, "Untitled.toml");

                GameScene defaultScene = new GameScene
                {
                    SceneName = "Untitled Scene"
                };

                // Create an anchor node to give the user something immediate to select in the hierarchy view
                GameObject globalSystemsGo = new GameObject
                {
                    Name = "Global Systems Anchor"
                };

                SceneSerializer.SaveScene(defaultScene, targetFilePath);

                Log.Info($"[Project Factory] Successfully baked template file asset: {targetFilePath}");
            }
            catch(Exception ex)
            {
                Log.Error($"[Project Factory Error] Failed to generate baseline Untitled.scene template. Reason: {ex.Message}");
            }
        }
    }
}