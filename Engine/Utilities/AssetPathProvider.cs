using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine.Core.Utilities
{
    public static class AssetPathProvider
    {
        /// <summary>
        /// The absolute path to the active game project directory.
        /// Defaults to a permanent sandbox folder outside of the bin folder during development.
        /// </summary>
        public static string ProjectRootPath
        {
            get; set;
        }

        static AssetPathProvider()
        {
            // WORKAROUND FOR DEVELOPMENT TRAFFIC:
            // Instead of saving inside bin/Debug, climb up out of the build folders 
            // and anchor into a permanent "GameProject" folder in your main workspace.
            string baseDir = AppDomain.CurrentDomain.BaseDirectory; // bin/Debug/net8.0-windows/

            // Navigate up 4 levels: net8.0-windows -> Debug -> bin -> Engine.Editor
            // Then drop it into a safe, permanent folder parallel to your projects.
            string repoRoot = Path.GetFullPath(Path.Combine(baseDir, @"..\..\..\..\"));

            ProjectRootPath = Path.Combine(repoRoot, "SavedGameProjectData");

            // Ensure the permanent sandbox directory physically exists on your drive
            if(!Directory.Exists(ProjectRootPath))
            {
                Directory.CreateDirectory(ProjectRootPath);
            }
        }

        /// <summary>
        /// Combines a relative asset path with the permanent project root path.
        /// Use this before sending any path to File.WriteAllText or File.ReadAllText.
        /// </summary>
        public static string ResolveProjectPath(string relativePath)
        {
            return Path.Combine(ProjectRootPath, relativePath);
        }

        /// <summary>
        /// Resolves paths for internal baseline engine content (Icons, Default Templates).
        /// These safely remain relative to the running executable application folder.
        /// </summary>
        public static string ResolveEnginePath(string relativePath)
        {
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "CoreContent", relativePath);
        }
    }
}
