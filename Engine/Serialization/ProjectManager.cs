using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Engine.Core.Serialization
{
    public class ProjectHistoryEntry
    {
        public string Name { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public DateTime LastOpened { get; set; } = DateTime.Now;

        public List<String> TargetPlatforms
        {
        get; set; } = new List<String>() { "Desktop"};
    }

    public static class ProjectHistoryStore
    {
        private static readonly string HistoryFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Tesseract2D",
            "recent_projects.json"
        );

        public static List<ProjectHistoryEntry> GetRecentProjects()
        {
            if(!File.Exists(HistoryFilePath))
                return new List<ProjectHistoryEntry>();
            try
            {
                string json = File.ReadAllText(HistoryFilePath);
                return JsonSerializer.Deserialize<List<ProjectHistoryEntry>>(json) ?? new List<ProjectHistoryEntry>();
            }
            catch
            {
                return new List<ProjectHistoryEntry>();
            }
        }

        public static void RecordProjectAccess(string projectName, string projectRootPath, List<string> selectedPlatforms)
        {
            var list = GetRecentProjects();

            // Remove existing entry if present so we can move it to the top as "Most Recent"
            list.RemoveAll(p => p.Path.Equals(projectRootPath, StringComparison.OrdinalIgnoreCase));

            list.Insert(0, new ProjectHistoryEntry
            {
                Name = projectName,
                Path = projectRootPath,
                TargetPlatforms = selectedPlatforms,
                LastOpened = DateTime.Now
            });

            string? dir = Path.GetDirectoryName(HistoryFilePath);
            if(!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            File.WriteAllText(HistoryFilePath, JsonSerializer.Serialize(list, new JsonSerializerOptions { WriteIndented = true }));
        }

        public static void RemoveFromHistory(string projectRootPath)
        {
            var list = GetRecentProjects();
            list.RemoveAll(p => p.Path.Equals(projectRootPath, StringComparison.OrdinalIgnoreCase));
            File.WriteAllText(HistoryFilePath, JsonSerializer.Serialize(list));
        }
    }
}
