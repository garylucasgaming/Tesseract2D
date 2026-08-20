using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace Engine.Editor.Utilities
{
    public class BuildResult
    {
        public bool Success
        {
            get; set;
        }
        public string OutputLog { get; set; } = string.Empty;
        public string AssemblyPath { get; set; } = string.Empty;
    }

    public static class ScriptCompiler
    {
        public static async Task<BuildResult> CompileGameplayProjectAsync(string projectRoot, string projectName)
        {
            // Fallback: If projectName wasn't passed or populated, derive it from the root folder name (e.g., "test4")
            if(string.IsNullOrWhiteSpace(projectName))
            {
                projectName = Path.GetFileName(projectRoot?.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
            }

            string csprojPath = Path.Combine(projectRoot, "Source", $"{projectName}.Gameplay", $"{projectName}.Gameplay.csproj");

            if(!File.Exists(csprojPath))
            {
                return new BuildResult
                {
                    Success = false,
                    OutputLog = $"Could not find project file at: {csprojPath}"
                };
            }

            // Prepare 'dotnet build' command
            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"build \"{csprojPath}\" -c Debug --no-incremental", // --no-incremental ensures clean output
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = Path.GetDirectoryName(csprojPath)
            };

            return await Task.Run(() =>
            {
                using(Process process = new Process { StartInfo = psi })
                {
                    process.Start();
                    string stdout = process.StandardOutput.ReadToEnd();
                    string stderr = process.StandardError.ReadToEnd();
                    process.WaitForExit();

                    bool success = process.ExitCode == 0;
                    string output = string.IsNullOrWhiteSpace(stderr) ? stdout : $"{stdout}\nERRORS:\n{stderr}";

                    // Path to the compiled output DLL
                    string dllPath = Path.Combine(
                        projectRoot,
                        "Source",
                        $"{projectName}.Gameplay",
                        "bin",
                        "Debug",
                        "net8.0",
                        $"{projectName}.Gameplay.dll"
                    );

                    ScriptAssemblyManager.ReloadProjectAssemblies();

                    return new BuildResult
                    {
                        Success = success && File.Exists(dllPath),
                        OutputLog = output,
                        AssemblyPath = dllPath
                    };
                }
            });
        }
    }
}