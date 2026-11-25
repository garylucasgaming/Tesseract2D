using System.IO;

namespace Engine.Core.Runtime
{
    /// <summary>
    /// Canonical layout for user projects. All editor consumers that locate
    /// gameplay scripts or compiler output should use these helpers rather than
    /// composing their own paths.
    /// </summary>
    public static class ProjectPaths
    {
        public static string GetScriptSourceRoot(string projectRoot)
        {
            return Path.Combine(projectRoot, "Content", "Assets");
        }

        public static string GetGameplayProjectDirectory(string projectRoot, string projectName)
        {
            return Path.Combine(projectRoot, "Source", $"{projectName}.Gameplay");
        }

        public static string GetGameplayProjectPath(string projectRoot, string projectName)
        {
            return Path.Combine(
                GetGameplayProjectDirectory(projectRoot, projectName),
                $"{projectName}.Gameplay.csproj");
        }

        public static string GetGameplayOutputDirectory(string projectRoot, string projectName)
        {
            return Path.Combine(
                GetGameplayProjectDirectory(projectRoot, projectName),
                "bin",
                "Debug",
                "net8.0");
        }

        public static string GetGameplayAssemblyPath(string projectRoot, string projectName)
        {
            return Path.Combine(
                GetGameplayOutputDirectory(projectRoot, projectName),
                $"{projectName}.Gameplay.dll");
        }
    }
}
